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
    <div style={{ maxWidth: 700, margin: '40px auto', fontFamily: 'system-ui' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h2>Samagra Assistant</h2>
        <button onClick={handleNewChat} disabled={messages.length === 0} style={{ padding: '6px 12px' }}>
          New chat
        </button>
      </div>

      <div style={{
        minHeight: 300, maxHeight: 500, overflowY: 'auto',
        border: '1px solid #e5e5e5', borderRadius: 8, padding: 16, marginBottom: 12
      }}>
        {messages.length === 0 && (
          <p style={{ color: '#999' }}>Ask about your orders or our policies.</p>
        )}

        {messages.map((m, i) => (
          <div key={i} style={{
            display: 'flex',
            justifyContent: m.role === 'user' ? 'flex-end' : 'flex-start',
            marginBottom: 10
          }}>
            <div style={{
              maxWidth: '80%', padding: '10px 14px', borderRadius: 12,
              background: m.role === 'user' ? '#2563eb' : '#f3f4f6',
              color: m.role === 'user' ? 'white' : '#111',
              whiteSpace: 'pre-wrap', lineHeight: 1.5
            }}>
              {m.text}
              {streaming && i === messages.length - 1 && m.role === 'assistant' && (
                <span style={{ opacity: 0.4 }}>▊</span>
              )}
            </div>
          </div>
        ))}
        <div ref={bottomRef} />
      </div>

      {error && <p style={{ color: '#c33' }}>{error}</p>}

      <div style={{ display: 'flex', gap: 8 }}>
        <input
          value={question}
          onChange={e => setQuestion(e.target.value)}
          onKeyDown={e => e.key === 'Enter' && handleAsk()}
          placeholder="Type your question..."
          disabled={streaming}
          style={{ flex: 1, padding: 10, fontSize: 15 }}
        />
        {streaming
          ? <button onClick={handleStop} style={{ padding: '10px 18px' }}>Stop</button>
          : <button onClick={handleAsk} style={{ padding: '10px 18px' }}>Ask</button>}
      </div>
    </div>
  );
}