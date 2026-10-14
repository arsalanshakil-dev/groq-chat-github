// Only provider fields enter the request; display annotations stay in the UI.
export function requestMessages(history, includeHistory) {
  return (includeHistory ? history : history.slice(-1))
    .map(({ role, content }) => ({ role, content }));
}
