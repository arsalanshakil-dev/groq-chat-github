param(
    [string]$PublishDirectory = './artifacts/monsterasp',
    [switch]$ValidateOnly
)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($env:MONSTERASP_PUBLISH_PROFILE)) {
    throw 'Add MONSTERASP_PUBLISH_PROFILE in GitHub Actions secrets with the full publishSettings XML.'
}
try { $xml = [xml]$env:MONSTERASP_PUBLISH_PROFILE }
catch { throw 'MONSTERASP_PUBLISH_PROFILE must contain valid publishSettings XML, not a filename or base64 value.' }
$profile = @($xml.publishData.publishProfile) | Where-Object { $_.publishMethod -eq 'MSDeploy' } | Select-Object -First 1
if (-not $profile) { throw 'The secret must contain an MSDeploy publish profile.' }
foreach ($attribute in @('publishUrl', 'msdeploySite', 'userName', 'userPWD')) {
    if ([string]::IsNullOrWhiteSpace($profile.GetAttribute($attribute))) {
        throw "The publish profile is missing $attribute. Download a fresh Web Deploy profile from MonsterASP."
    }
}
$source = (Resolve-Path -LiteralPath $PublishDirectory).Path
if (-not (Test-Path (Join-Path $source 'ChatApi.dll')) -or -not (Test-Path (Join-Path $source 'web.config'))) {
    throw 'Published backend files are missing ChatApi.dll or web.config.'
}
$server = [string]$profile.publishUrl
if ($server -notmatch '^https?://') { $server = "https://${server}" }
$endpoint = [UriBuilder]$server
if ($endpoint.Scheme -ne 'https') { throw 'Web Deploy requires an HTTPS endpoint.' }
if ($endpoint.Port -eq 443) { $endpoint.Port = 8172 }
if ($endpoint.Path -eq '/') { $endpoint.Path = '/msdeploy.axd' }
$endpoint.Query = 'site=' + [Uri]::EscapeDataString([string]$profile.msdeploySite)
if ($ValidateOnly) { Write-Host 'Publish profile and backend files validated. No deployment performed.'; return }
$exe = 'C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe'
if (-not (Test-Path $exe)) {
    $exe = 'C:\Program Files (x86)\IIS\Microsoft Web Deploy V3\msdeploy.exe'
}
if (-not (Test-Path $exe)) { throw 'Web Deploy is not installed on this Windows runner.' }
# Register individual credentials too: GitHub may otherwise only mask the whole XML.
if ($env:GITHUB_ACTIONS -eq 'true') {
    foreach ($credential in @([string]$profile.userName, [string]$profile.userPWD)) {
        $masked = $credential.Replace('%', '%25').Replace("`r", '%0D').Replace("`n", '%0A')
        Write-Host "::add-mask::$masked"
    }
}
$destination = "-dest:contentPath='$($profile.msdeploySite)',computerName='$($endpoint.Uri.AbsoluteUri)',userName='$($profile.userName)',password='$($profile.userPWD)',authType='Basic'"
# Keep credentials in memory, not in the repository or a generated profile file.
$deploymentOutput = & $exe '-verb:sync' "-source:contentPath=$source" $destination '-enableRule:AppOffline' '-enableRule:DoNotDeleteRule' 2>&1
$deploymentExit = $LASTEXITCODE
foreach ($line in $deploymentOutput) {
    $safeLine = "$line".Replace([string]$profile.userPWD, '[REDACTED]').Replace([string]$profile.userName, '[REDACTED]')
    Write-Host $safeLine
}
if ($deploymentExit -ne 0) { throw "Web Deploy failed with exit code $deploymentExit. Check the deployment output and MonsterASP WebDeploy settings." }
Write-Host 'MonsterASP backend deployed successfully.'
