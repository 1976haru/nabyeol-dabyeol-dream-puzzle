import { loadSave } from '../save/saveManager';
import { ALL_QUIZ_QUESTIONS, getQuizQuestion } from '../data/quizzes';
import { STAGES } from '../data/stages';

interface Props {
  onBack: () => void;
}

/** 부모용 학습 보고서 (법무부 버전) */
export function ParentReportScreen({ onBack }: Props) {
  const save = loadSave();
  const learned = save.educationProgress
    .map((id) => getQuizQuestion(id))
    .filter((q): q is NonNullable<typeof q> => q !== undefined);

  const stats: { label: string; value: string }[] = [
    { label: '클리어한 스테이지', value: `${save.clearedStages.length} / ${STAGES.length}` },
    { label: '배운 법 상식', value: `${learned.length} / ${ALL_QUIZ_QUESTIONS.length}` },
    { label: '도전한 최고 스테이지', value: `${save.lastStageId}` },
  ];

  return (
    <div style={{ minHeight: '100vh', padding: 20, boxSizing: 'border-box' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, maxWidth: 480, margin: '0 auto' }}>
        <button type="button" onClick={onBack} style={backBtn}>
          ← 뒤로
        </button>
        <h2 style={{ color: 'white', margin: 0, fontSize: 22 }}>👪 학습 보고서</h2>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 10, maxWidth: 480, margin: '18px auto 0' }}>
        {stats.map((s) => (
          <div key={s.label} style={statCard}>
            <div style={{ fontSize: 20, fontWeight: 800, color: '#1c2b4a' }}>{s.value}</div>
            <div style={{ fontSize: 12, color: '#868e96', marginTop: 4 }}>{s.label}</div>
          </div>
        ))}
      </div>

      <h3 style={{ color: 'white', maxWidth: 480, margin: '24px auto 8px', fontSize: 17 }}>
        ⚖️ 아이가 배운 내용
      </h3>
      <div style={{ maxWidth: 480, margin: '0 auto', display: 'flex', flexDirection: 'column', gap: 10 }}>
        {learned.length === 0 ? (
          <div style={{ ...listItem, color: '#868e96', textAlign: 'center' }}>
            아직 배운 내용이 없어요. 법 퀴즈에 도전해 보세요!
          </div>
        ) : (
          learned.map((q) => (
            <div key={q.id} style={listItem}>
              <div style={{ fontWeight: 700, color: '#1c2b4a' }}>✅ {q.question}</div>
              <div style={{ fontSize: 14, color: '#495057', marginTop: 4 }}>{q.explanation}</div>
            </div>
          ))
        )}
      </div>
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

const statCard: React.CSSProperties = {
  background: 'white',
  borderRadius: 16,
  padding: 14,
  textAlign: 'center',
};

const listItem: React.CSSProperties = {
  background: 'white',
  borderRadius: 14,
  padding: '12px 16px',
};
