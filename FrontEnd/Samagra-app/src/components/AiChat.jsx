 import { useState, useRef, useEffect } from 'react';
import { fetchEventSource } from '@microsoft/fetch-event-source';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5109/api';

export default function AiChat() {
  const [question, setQuestion] = useState('');
  const [messages, setMessages] = useState([]);          // { role: 'user' | 'assistant', text }
  const [conversationId, setConversationId] = useState(null);
  const [streaming, setStreaming] = useState(false);
  const [error, setError] = useState('');
  const controllerRef = useRef(null);
  const bottomRef = useRef(null);

  // नया text आए तो नीचे scroll
  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  async function handleAsk() {
    const text = question.trim();
    if (!text || streaming) return;

    setError('');
    setQuestion('');
    setStreaming(true);

    // user का message, और assistant के लिए खाली जगह
    setMessages(prev => [
      ...prev,
      { role: 'user', text },
      { role: 'assistant', text: '' }
    ]);

    const controller = new AbortController();
    controllerRef.current = controller;

    try {
      await fetchEventSource(`${API_URL}/ai/stream`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ conversationId, question: text }),
        credentials: 'include',
        signal: controller.signal,
        openWhenHidden: true,

        onopen(res) {
          if (res.ok) return;
          throw new Error(res.status === 401 ? 'Please sign in first.' : `Server error ${res.status}`);
        },

        onmessage(ev) {
          if (ev.event === 'conversation') {
            setConversationId(ev.data);
          } else if (ev.event === 'token') {
            // आख़िरी message (assistant वाला) में token जोड़ो
            setMessages(prev => {
              const next = [...prev];
              const last = next[next.length - 1];
              next[next.length - 1] = { ...last, text: last.text + ev.data };
              return next;
            });
          } else if (ev.event === 'error') {
            setError(ev.data);
          } else if (ev.event === 'done') {
            controller.abort();
          }
        },

        onerror(err) {
          throw err;
        },
      });
    } catch (err) {
      if (err.name !== 'AbortError') setError(err.message);
    } finally {
      setStreaming(false);
      controllerRef.current = null;
    }
  }

  function handleStop() {
    controllerRef.current?.abort();
    setStreaming(false);
  }

  function handleNewChat() {
    controllerRef.current?.abort();
    setMessages([]);
    setConversationId(null);
    setError('');
  }

  return (
    <div className="mx-auto max-w-2xl">
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-ink">Samagra Assistant</h1>
          <p className="mt-1 text-sm text-ink-muted">
            Ask about your orders or our policies.
          </p>
        </div>
        <button
          onClick={handleNewChat}
          disabled={messages.length === 0}
          className="rounded-control border border-line bg-surface px-4 py-2 text-sm font-semibold text-ink transition-colors hover:bg-surface-muted disabled:opacity-40 disabled:hover:bg-surface"
        >
          New chat
        </button>
      </div>

      <div className="mb-3 max-h-[60vh] min-h-[18rem] overflow-y-auto rounded-card border border-line bg-surface p-4 sm:max-h-[32rem]">
        {messages.length === 0 && (
          <p className="py-10 text-center text-sm text-ink-subtle">
            Try “where is my last order?” or “what is the return window?”
          </p>
        )}

        {messages.map((m, i) => (
          <div
            key={i}
            className={`mb-2.5 flex ${m.role === 'user' ? 'justify-end' : 'justify-start'}`}
          >
            <div
              className={`max-w-[85%] whitespace-pre-wrap rounded-card px-3.5 py-2.5 text-[15px] leading-relaxed sm:max-w-[80%] ${
                m.role === 'user'
                  ? 'bg-brand-600 text-ink-inverse'
                  : 'bg-surface-muted text-ink'
              }`}
            >
              {m.text}
              {streaming && i === messages.length - 1 && m.role === 'assistant' && (
                <span className="opacity-40">▊</span>
              )}
            </div>
          </div>
        ))}
        <div ref={bottomRef} />
      </div>

      {error && (
        <p
          role="alert"
          className="mb-3 rounded-control border border-danger-200 bg-danger-50 px-4 py-3 text-sm text-danger-700"
        >
          {error}
        </p>
      )}

      <div className="flex gap-2">
        <input
          value={question}
          onChange={e => setQuestion(e.target.value)}
          onKeyDown={e => e.key === 'Enter' && handleAsk()}
          placeholder="Type your question…"
          disabled={streaming}
          className="flex-1 rounded-control border border-line bg-surface px-4 py-3 text-[15px] text-ink placeholder:text-ink-subtle focus:border-brand-600 focus:outline-none disabled:bg-surface-muted"
        />
        {streaming ? (
          <button
            onClick={handleStop}
            className="shrink-0 rounded-control border border-line bg-surface px-5 py-3 text-sm font-bold text-ink transition-colors hover:bg-surface-muted"
          >
            Stop
          </button>
        ) : (
          <button
            onClick={handleAsk}
            className="shrink-0 rounded-control bg-brand-600 px-6 py-3 text-sm font-bold text-ink-inverse transition-colors hover:bg-brand-700"
          >
            Ask
          </button>
        )}
      </div>
    </div>
  );
}