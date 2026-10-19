// src/hooks/useAsyncData.js
//
// GET + loading + error + retry ka wahi pattern har screen par dohराना padta tha.
// Dhyan rakhne wali do baatein:
//   1. `fetcher` ko useCallback se memoize karein, warna har render par refetch hoga.
//   2. Pehla setState await ke baad hota hai — eslint ka set-state-in-effect rule
//      effect body mein synchronous setState allow nahi karta.

import { useCallback, useEffect, useState } from 'react';
import { errorMessage } from '../lib/format';

export default function useAsyncData(fetcher, { initialData = null, fallbackError } = {}) {
  const [data, setData] = useState(initialData);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let active = true;

    (async () => {
      try {
        const result = await fetcher();
        if (!active) return;
        setData(result);
        setError('');
      } catch (err) {
        if (!active) return;
        setError(errorMessage(err, fallbackError));
      } finally {
        // Unmount ya purani request ke baad state chhedna nahi hai
        if (active) setLoading(false);
      }
    })();

    return () => {
      active = false;
    };
  }, [fetcher, reloadKey, fallbackError]);

  const reload = useCallback(() => {
    setLoading(true);
    setError('');
    setReloadKey((key) => key + 1);
  }, []);

  return { data, loading, error, reload };
}
