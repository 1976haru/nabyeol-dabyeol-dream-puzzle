interface Props {
  status: 'clear' | 'fail';
  score: number;
  targetScore: number;
  message: string;
  onRetry: () => void;
  onNext?: () => void;
  onMenu: () => void;
}

/** 클리어/실패 결과 팝업 (전체화면 오버레이) */
export function ResultPopup({ status, score, targetScore, message, onRetry, onNext, onMenu }: Props) {
  const isClear = status === 'clear';
  return (
    <div
      style={{
        position: 'fixed',
        inset: 0,
        background: 'rgba(0,0,0,0.6)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        zIndex: 50,
        padding: 16,
      }}
    >
      <div
        style={{
          background: 'white',
          borderRadius: 24,
          padding: 32,
          width: '100%',
          maxWidth: 360,
          textAlign: 'center',
          boxShadow: '0 12px 40px rgba(0,0,0,0.3)',
        }}
      >
        <div style={{ fontSize: 64, animation: 'pop 0.5s ease' }}>{isClear ? '🎉' : '😢'}</div>
        <h2 style={{ margin: '12px 0', fontSize: 24, color: isClear ? '#f08c00' : '#495057' }}>
          {isClear ? '스테이지 클리어!' : '아쉬워요!'}
        </h2>
        <p style={{ fontSize: 17, color: '#495057', margin: '8px 0 4px' }}>{message}</p>
        <p style={{ fontSize: 16, color: '#868e96', marginBottom: 20 }}>
          점수 {score} / {targetScore}
        </p>

        <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          {isClear && onNext && (
            <button type="button" onClick={onNext} style={primaryBtn}>
              다음 스테이지 ▶
            </button>
          )}
          {!isClear && (
            <button type="button" onClick={onRetry} style={primaryBtn}>
              다시 도전 🔄
            </button>
          )}
          <button type="button" onClick={onMenu} style={secondaryBtn}>
            메뉴로
          </button>
        </div>
      </div>
    </div>
  );
}

const primaryBtn: React.CSSProperties = {
  background: 'linear-gradient(90deg,#748ffc,#5c7cfa)',
  color: 'white',
  border: 'none',
  borderRadius: 16,
  padding: '14px',
  fontSize: 18,
  fontWeight: 700,
  cursor: 'pointer',
};

const secondaryBtn: React.CSSProperties = {
  background: '#f1f3f5',
  color: '#495057',
  border: 'none',
  borderRadius: 16,
  padding: '12px',
  fontSize: 16,
  fontWeight: 600,
  cursor: 'pointer',
};
