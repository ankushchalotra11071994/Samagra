import { useState, useRef } from 'react';
import { fetchEventSource } from '@microsoft/fetch-event-source';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5109/api';

export default function AiChat() {
  const [question, setQuestion] = useState('');
  const [answer, setAnswer] = useState('');
  const [streaming, setStreaming] = useState(false);
  const [error, setError] = useState('');
  const controllerRef = useRef(null);

  async function handleAsk() {
    if (!question.trim() || streaming) return;

    setAnswer('');
    setError('');
    setStreaming(true);

    const controller = new AbortController();
    controllerRef.current = controller;

    try {
      await fetchEventSource(`${API_URL}/ai/stream`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ question }),
        credentials: 'include',
        signal: controller.signal,
        openWhenHidden: true,

        onopen(res) {
          if (res.ok) return;
          throw new Error(res.status === 401 ? 'Please sign in first.' : `Server error ${res.status}`);
        },

        onmessage(ev) {
          if (ev.event === 'token') {
            setAnswer(prev => prev + ev.data);
          } else if (ev.event === 'error') {
            setError(ev.data);
          } else if (ev.event === 'done') {
            controller.abort();
          }
        },

        onerror(err) {
          throw err;   // throw करना ज़रूरी — वरना library दोबारा try करती रहेगी
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

  return (
    <div style={{ maxWidth: 700, margin: '40px auto', fontFamily: 'system-ui' }}>
      <h2>Samagra Assistant</h2>

      <div style={{ display: 'flex', gap: 8 }}>
        <input
          value={question}
          onChange={e => setQuestion(e.target.value)}
          onKeyDown={e => e.key === 'Enter' && handleAsk()}
          placeholder="Ask about your orders or our policies..."
          disabled={streaming}
          style={{ flex: 1, padding: 10, fontSize: 15 }}
        />
        {streaming
          ? <button onClick={handleStop} style={{ padding: '10px 18px' }}>Stop</button>
          : <button onClick={handleAsk} style={{ padding: '10px 18px' }}>Ask</button>}
      </div>

      {error && (
        <p style={{ color: '#c33', marginTop: 16 }}>{error}</p>
      )}

      {(answer || streaming) && (
        <div style={{
          marginTop: 20, padding: 16, background: '#f6f6f6',
          borderRadius: 8, whiteSpace: 'pre-wrap', lineHeight: 1.6
        }}>
          {answer}
          {streaming && <span style={{ opacity: 0.4 }}>▊</span>}
        </div>
      )}
    </div>
  );
}