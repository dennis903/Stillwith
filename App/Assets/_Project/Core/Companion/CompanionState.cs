namespace StudyMate.Core.Companion
{
    /// <summary>캐릭터 상태 (기획서 8.3). Idle은 세션이 시작되기 전이다.</summary>
    public enum CompanionState
    {
        /// <summary>세션 전. 아직 첫 착석이 없거나, 세션이 끝난 뒤.</summary>
        Idle,
        /// <summary>함께하기 — 내가 착석해 있음. 집중 지점에서 집중 활동.</summary>
        Together,
        /// <summary>기다리기 — 잠깐 이탈함. 집중 지점에서 하던 일을 멈추고 풍경을 바라봄.</summary>
        Waiting,
        /// <summary>쉬기 — 휴식 제안을 수락했거나 오래 이탈함. 쉼 지점에서 쉬는 활동.</summary>
        Resting,
        /// <summary>돌아오기 — 쉬기 중 다시 착석함. 이동 경로로 집중 지점에 걸어감.</summary>
        Returning,
    }

    /// <summary>쉬기에 들어간 이유. 기록 항목 "쉼 횟수와 이유"(기획서 8.9)에 쓴다.</summary>
    public enum RestReason
    {
        /// <summary>휴식 제안에서 [산책하기]를 눌렀거나, 제안 중 30초 넘게 이탈함.</summary>
        BreakAccepted,
        /// <summary>5분 넘게 이탈함.</summary>
        LongAbsence,
    }

    /// <summary>세션이 끝난 이유 (기획서 8.8).</summary>
    public enum SessionEndReason
    {
        /// <summary>"오늘은 여기까지" 버튼.</summary>
        UserEnded,
        /// <summary>30분 연속 이탈. 요약은 다음에 앱을 켤 때 보여 준다.</summary>
        LongAbsence,
    }
}
