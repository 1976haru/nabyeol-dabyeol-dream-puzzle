import { useState } from 'react';
import type { ScreenName } from './navigation';
import { MenuScreen } from './screens/MenuScreen';
import { StageSelectScreen } from './screens/StageSelectScreen';
import { CollectionScreen } from './screens/CollectionScreen';
import { GameScreen } from './screens/GameScreen';
import { InstallBanner } from './components/InstallBanner';

export default function App() {
  const [screen, setScreen] = useState<ScreenName>('menu');
  const [stageId, setStageId] = useState(1);

  const navigate = (next: ScreenName, id?: number) => {
    if (id !== undefined) setStageId(id);
    setScreen(next);
  };

  return (
    <div className="app-root">
      {screen === 'menu' && <MenuScreen onNavigate={navigate} />}
      {screen === 'stage-select' && (
        <StageSelectScreen onSelect={(id) => navigate('game', id)} onBack={() => setScreen('menu')} />
      )}
      {screen === 'collection' && <CollectionScreen onBack={() => setScreen('menu')} />}
      {screen === 'game' && (
        <GameScreen
          key={stageId}
          stageId={stageId}
          onNext={(id) => navigate('game', id)}
          onMenu={() => setScreen('menu')}
        />
      )}
      <InstallBanner />
    </div>
  );
}
