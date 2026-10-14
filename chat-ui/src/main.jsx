import React, { useEffect, useRef, useState } from 'react';
import { createRoot } from 'react-dom/client';
import './style.css';
import { requestMessages } from './chat-context.js';

function App() {
  const [messages, setMessages] = useState([]);
  const [draft, setDraft] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [tokens, setTokens] = useState(null);
  const [includeHistory, setIncludeHistory] = useState(true);
  const bottom = useRef(null);
  const input = useRef(null);
  const controller = useRef(null);
  useEffect(() => {
    if (messages.length || busy) bottom.current?.scrollIntoView({ behavior: 'smooth' });
    else bottom.current?.parentElement?.scrollTo({ top: 0 });
  }, [messages, busy]);
  useEffect(() => () => controller.current?.abort(), []);
  async function send(text = draft) {
    const content = text.trim();
    if (!content || busy) return;
    const history = [...messages, { role: 'user', content }];
    const context = requestMessages(history, includeHistory);
    if (context.length > 40) { setError('Start a new chat or turn off history to continue. This request has reached its message limit.'); return; }
    setMessages(history); setDraft(''); setError(''); setBusy(true);
    controller.current = new AbortController();
    try {
      const apiBase = (import.meta.env.VITE_API_BASE_URL || '').replace(/\/$/, '');
      const response = await fetch(`${apiBase}/api/chat`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ messages: context }), signal: controller.current.signal
      });
      const data = await response.json();
      if (!response.ok) throw new Error(data.error || 'Something went wrong. Please try again.');
      setMessages([...history, { role: 'assistant', content: data.reply, contextCount: context.length }]);
      setTokens(data.usage?.total_tokens ?? null);
    } catch (e) {
      setMessages(history.slice(0, -1)); setDraft(content);
      setError(e.name === 'AbortError' ? 'Request stopped. Your message is ready to send again.' : e.message);
    } finally { setBusy(false); controller.current = null; input.current?.focus(); }
  }
  function reset() { setMessages([]); setDraft(''); setError(''); setTokens(null); input.current?.focus(); }
  return <div className="shell">
    <aside><a className="brand" href="/">g<span>Groq Chat</span></a><button className="new-chat" disabled={busy} onClick={reset}>＋ New chat</button><div className="sidebar-note"><span className="dot"/> Powered by Groq<p>A little space for big ideas.</p></div></aside>
    <main><header><div><span className="eyebrow">YOUR EVERYDAY ASSISTANT</span><h1>Let’s think together.</h1></div><span className="badge">Groq</span></header>
      <div className="context-mode">
        <label className="history-switch"><input type="checkbox" role="switch" checked={includeHistory} disabled={busy} onChange={e => setIncludeHistory(e.target.checked)} aria-describedby="context-note"/><span>Send chat history</span><strong>{includeHistory ? 'On' : 'Off'}</strong></label>
        <p id="context-note">{includeHistory ? 'Earlier messages are sent with your next message to give the model context.' : 'Only your latest message is sent. Earlier messages stay visible here, but the model does not receive them.'}</p>
        <details><summary>Try a memory experiment</summary><p>Say “My name is Sam.” Then ask “What is my name?” with history on and off. The model generates a response from the text it receives; this app supplies conversation context. Without that context, it cannot know what you said earlier (though it may guess).</p></details>
      </div>
      <section className="conversation" aria-label="Conversation" aria-live="polite" aria-busy={busy}>
        {!messages.length && <div className="welcome"><div className="spark">✳</div><h2>What’s on your mind?</h2><p>Ask a question, untangle an idea, or make something new.</p><div className="suggestions">{['Explain a C# concept simply', 'Help me plan a small project', 'Give me a creative writing prompt'].map(t => <button disabled={busy} key={t} onClick={() => send(t)}>{t}<span>↗</span></button>)}</div></div>}
        {messages.map((m, i) => <article key={i} className={`message ${m.role}`}><span className="avatar">{m.role === 'user' ? 'You' : 'g'}</span><div><span className="message-label">{m.role === 'user' ? 'You' : 'Groq'} </span>{m.role === 'assistant' && <span className="context-count">{m.contextCount === 1 ? 'Latest message only' : `${m.contextCount} messages sent`}</span>}<p>{m.content}</p></div></article>)}
        {busy && <div className="thinking"><span className="dot"/> Thinking…</div>}<div ref={bottom}/>
      </section>
      <footer>{error && <div className="error" role="alert">{error}</div>}<form onSubmit={e => { e.preventDefault(); send(); }}><label className="sr-only" htmlFor="prompt">Your message</label><textarea id="prompt" ref={input} value={draft} disabled={busy} maxLength={12000} rows={2} placeholder="Ask anything…" onChange={e => setDraft(e.target.value)} onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey && !e.nativeEvent.isComposing) { e.preventDefault(); send(); } }}/>{busy ? <button type="button" className="send" onClick={() => controller.current?.abort()} aria-label="Stop request">■</button> : <button className="send" disabled={!draft.trim()} aria-label="Send message">↑</button>}</form><div className="footer-meta"><span>Enter to send · Shift + Enter for a new line</span><span>{tokens != null ? `${tokens.toLocaleString()} tokens · last request` : 'AI can make mistakes. Check important details.'}</span></div></footer>
    </main>
  </div>;
}
createRoot(document.getElementById('root')).render(<App/>);
