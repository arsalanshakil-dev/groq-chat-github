"""Deploy published ASP.NET files over explicit FTPS; credentials stay in env vars."""
import ftplib
import io
import os
from pathlib import Path
import ssl
import sys
import time


def deploy(local_dir):
    source = Path(local_dir).resolve()
    if not (source / 'ChatApi.dll').is_file() or not (source / 'web.config').is_file():
        raise ValueError('Publish output must contain ChatApi.dll and web.config.')
    required = ['FTP_SERVER', 'FTP_USERNAME', 'FTP_PASSWORD']
    if any(not os.environ.get(name) for name in required):
        raise ValueError('Set MONSTERASP_FTP_SERVER, MONSTERASP_FTP_USERNAME, and MONSTERASP_FTP_PASSWORD in GitHub Secrets.')
    # Certificate verification and encrypted control/data channels are mandatory.
    ftp = ftplib.FTP_TLS(context=ssl.create_default_context(), timeout=60)
    offline = False
    try:
        ftp.connect(os.environ['FTP_SERVER'], int(os.environ.get('FTP_PORT', '21')))
        ftp.login(os.environ['FTP_USERNAME'], os.environ['FTP_PASSWORD'])
        ftp.prot_p()
        ftp.cwd(os.environ.get('FTP_DIRECTORY') or '/wwwroot/')
        remote_root = ftp.pwd()
        # Do not remove someone else's maintenance file.
        if 'app_offline.htm' in [name.rsplit('/', 1)[-1] for name in ftp.nlst()]:
            raise ValueError('app_offline.htm already exists. Resolve the existing maintenance operation first.')
        ftp.storbinary('STOR app_offline.htm', io.BytesIO(b'<h1>Updating Groq Chat. Please check back shortly.</h1>'))
        offline = True
        time.sleep(10)  # IIS must release DLL file locks before upload.
        for file in sorted(source.rglob('*')):
            if not file.is_file():
                continue
            relative = file.relative_to(source)
            if file.name == 'app_offline.htm':
                continue
            ftp.cwd(remote_root)
            for folder in relative.parts[:-1]:
                try:
                    ftp.cwd(folder)
                except ftplib.error_perm as error:
                    if not str(error).startswith('550'):
                        raise
                    ftp.mkd(folder)
                    ftp.cwd(folder)
            with file.open('rb') as data:
                ftp.storbinary('STOR ' + file.name, data)
            print('Uploaded ' + relative.as_posix())
        print('Backend upload finished. Existing unrelated server files were preserved.')
    finally:
        try:
            if offline:
                ftp.cwd(remote_root)
                ftp.delete('app_offline.htm')
                print('Removed maintenance file; IIS can restart the application.')
        finally:
            ftp.close()


if __name__ == '__main__':
    try:
        deploy(sys.argv[1] if len(sys.argv) > 1 else 'artifacts/monsterasp')
    except Exception as error:
        # Avoid printing FTP responses or tracebacks that could include credentials.
        print('Deployment failed (' + type(error).__name__ + '). Check credentials, FTPS certificate, remote directory, and MonsterASP logs.', file=sys.stderr)
        print('If the connection dropped during deployment, remove app_offline.htm in the host file manager before restarting.', file=sys.stderr)
        sys.exit(1)
