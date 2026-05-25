import { useState } from 'react';

const KEY = 'install_shown';

/** 홈화면 추가 안내 배너 (한 번만 표시) */
export function InstallBanner() {
  const [hidden, setHidden] = useState(() => {
    try {
      return localStorage.getItem(KEY) !== null;
    } catch {
      return true;
    }
  });

  if (hidden) return null;

  const close = () => {
    try {
      localStorage.setItem(KEY, '1');
    } catch {
      // ignore
    }
    setHidden(true);
  };

  return (
    <div
      style={{
        position: 'fixed',
        bottom: 12,
        left: 12,
        right: 12,
        background: 'rgba(33,37,41,0.95)',
        color: 'white',
        borderRadius: 14,
        padding: '12px 14px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        gap: 10,
        zIndex: 40,
        fontSize: 15,
      }}
    >
      <span>홈화면에 추가하면 앱처럼 쓸 수 있어요! 📱</span>
      <button
        type="button"
        onClick={close}
        aria-label="닫기"
        style={{
          background: 'rgba(255,255,255,0.2)',
          color: 'white',
          border: 'none',
          borderRadius: 8,
          width: 32,
          height: 32,
          fontSize: 18,
          cursor: 'pointer',
          flexShrink: 0,
        }}
      >
        ✕
      </button>
    </div>
  );
}
