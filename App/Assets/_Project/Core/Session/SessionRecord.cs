using System;
using StudyMate.Core.Companion;

namespace StudyMate.Core.Session
{
    /// <summary>
    /// 끝난 세션 한 건의 기록 (기획서 8.9). 기기 안에 저장한다.
    /// 쓴 장소와 캐릭터는 장소 선택 기능을 만들 때 추가한다.
    /// </summary>
    [Serializable]
    public sealed class SessionRecord
    {
        public DateTime startedAt;
        public DateTime endedAt;
        public double togetherSeconds;
        public double longestStretchSeconds;
        public int restsBreakAccepted;
        public int restsLongAbsence;
        public SessionEndReason endReason;

        /// <summary>이 세션이 속한 공부한 날.</summary>
        public DateTime Day => StudyDay.Of(startedAt);
    }
}
