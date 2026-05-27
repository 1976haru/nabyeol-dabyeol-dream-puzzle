import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';
import App from './App.tsx';
import { applyThemeCss } from './themes';

// 활성 테마의 배경/강조색을 CSS 변수로 주입 (렌더 전에 1회)
applyThemeCss();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);

// PWA: 서비스 워커 등록 (프로덕션 빌드에서만)
if ('serviceWorker' in navigator && import.meta.env.PROD) {
  window.addEventListener('load', () => {
    const swUrl = `${import.meta.env.BASE_URL}sw.js`;
    navigator.serviceWorker.register(swUrl).catch(() => {
      // 등록 실패는 무시 (게임은 계속 동작)
    });
  });
}
