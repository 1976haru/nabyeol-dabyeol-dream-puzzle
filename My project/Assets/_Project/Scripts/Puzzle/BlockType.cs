namespace NabyeolDabyeolDreamPuzzle.Puzzle
{
    /// <summary>
    /// 3매치 퍼즐 보드에서 사용하는 블록 타입.
    /// </summary>
    public enum BlockType
    {
        /// <summary>
        /// 비어 있는 칸. 매치 제거 후 공백, 낙하 처리, null 대체 상태를 표현한다.
        /// 일반 랜덤 생성 대상에서 제외한다.
        /// </summary>
        Empty = 0,

        /// <summary>
        /// 꿈방울 블록. 일반 랜덤 생성 대상.
        /// </summary>
        DreamBubble = 1,

        /// <summary>
        /// 달떡 블록. 일반 랜덤 생성 대상.
        /// </summary>
        MoonRiceCake = 2,

        /// <summary>
        /// 잉크별 블록. 일반 랜덤 생성 대상.
        /// </summary>
        InkStar = 3,

        /// <summary>
        /// 물결구름 블록. 일반 랜덤 생성 대상.
        /// </summary>
        WaveCloud = 4,

        /// <summary>
        /// 마음빛 블록. 일반 랜덤 생성 대상.
        /// </summary>
        HeartLight = 5,

        /// <summary>
        /// 장난구름/방해 블록. 일반 랜덤 생성 대상에서 제외한다.
        /// </summary>
        Noise = 6
    }
}
