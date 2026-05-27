import { useState } from 'react';
import { ALL_QUIZ_QUESTIONS } from '../data/quizzes';
import { markEducation } from '../save/saveManager';
import { soundEngine } from '../audio/soundEngine';

interface Props {
  onBack: () => void;
}

/** 법무부 버전 OX 퀴즈 화면 */
export function QuizScreen({ onBack }: Props) {
  const [index, setIndex] = useState(0);
  const [picked, setPicked] = useState<boolean | null>(null);
  const [correctCount, setCorrectCount] = useState(0);

  const question = ALL_QUIZ_QUESTIONS[index];
  const finished = index >= ALL_QUIZ_QUESTIONS.length;

  const answer = (value: boolean) => {
    if (picked !== null) return;
    soundEngine.unlock();
    setPicked(value);
    const isCorrect = value === question.isTrue;
    if (isCorrect) {
      setCorrectCount((c) => c + 1);
      markEducation(question.id);
      soundEngine.playMatch(1);
    } else {
      soundEngine.playInvalid();
    }
    setTimeout(() => {
      setPicked(null);
      setIndex((i) => i + 1);
    }, 2500);
  };

  return (
    <div style={{ minHeight: '100vh', padding: 20, boxSizing: 'border-box' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, maxWidth: 480, margin: '0 auto' }}>
        <button type="button" onClick={onBack} style={backBtn}>
          ← 뒤로
        </button>
        <h2 style={{ color: 'white', margin: 0, fontSize: 22 }}>⚖️ 법 퀴즈</h2>
      </div>

      {finished ? (
        <div style={card}>
          <div style={{ fontSize: 48 }}>🎉</div>
          <p style={{ fontSize: 20, fontWeight: 800, color: '#1c2b4a', margin: '8px 0' }}>
            퀴즈 완료!
          </p>
          <p style={{ fontSize: 17, color: '#495057' }}>
            {ALL_QUIZ_QUESTIONS.length}문제 중 {correctCount}문제를 맞혔어요.
          </p>
          <button type="button" style={primary} onClick={onBack}>
            메뉴로 돌아가기
          </button>
        </div>
      ) : (
        <>
          <p style={{ color: 'rgba(255,255,255,0.9)', textAlign: 'center', marginTop: 16 }}>
            {index + 1} / {ALL_QUIZ_QUESTIONS.length}
          </p>
          <div style={card}>
            <p style={{ fontSize: 19, fontWeight: 700, color: '#1c2b4a', minHeight: 56, margin: 0 }}>
              {question.question}
            </p>

            {picked === null ? (
              <div style={{ display: 'flex', gap: 16, marginTop: 16 }}>
                <button type="button" style={oButton} onClick={() => answer(true)} aria-label="맞아요">
                  ⭕
                </button>
                <button type="button" style={xButton} onClick={() => answer(false)} aria-label="아니에요">
                  ❌
                </button>
              </div>
            ) : (
              <div style={{ marginTop: 12 }}>
                <p style={{ fontSize: 22, fontWeight: 800, color: picked === question.isTrue ? '#2f9e44' : '#e03131' }}>
                  {picked === question.isTrue ? '정답이에요! 🎯' : '아쉬워요!'}
                </p>
                <p style={{ fontSize: 16, color: '#495057', lineHeight: 1.6 }}>{question.explanation}</p>
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
}

const backBtn: React.CSSProperties = {
  background: 'rgba(255,255,255,0.22)',
  color: 'white',
  border: 'none',
  borderRadius: 10,
  padding: '8px 14px',
  fontSize: 15,
  cursor: 'pointer',
  fontWeight: 600,
};

const card: React.CSSProperties = {
  background: 'white',
  borderRadius: 20,
  padding: 24,
  maxWidth: 420,
  margin: '20px auto 0',
  textAlign: 'center',
};

const baseChoice: React.CSSProperties = {
  flex: 1,
  fontSize: 44,
  padding: '18px 0',
  border: 'none',
  borderRadius: 18,
  cursor: 'pointer',
};

const oButton: React.CSSProperties = { ...baseChoice, background: '#e7f5ff' };
const xButton: React.CSSProperties = { ...baseChoice, background: '#fff0f0' };

const primary: React.CSSProperties = {
  marginTop: 16,
  background: 'linear-gradient(90deg,#c9a227,#4080c0)',
  color: 'white',
  border: 'none',
  borderRadius: 16,
  padding: '14px 22px',
  fontSize: 17,
  fontWeight: 800,
  cursor: 'pointer',
};
