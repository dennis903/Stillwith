using System;
using UnityEngine;

namespace StudyMate.Core.Companion
{
    /// <summary>
    /// 감지 판정 규칙 (기획서 8.4, 8.7). 모두 초기값이며 써 보며 조정한다. 단위는 초.
    /// </summary>
    [Serializable]
    public sealed class PresenceRules
    {
        [Tooltip("있음이 이만큼 이어지면 함께하기 (쉬기 중이면 돌아오기)")]
        public float presentToTogether = 10f;

        [Tooltip("없음이 이만큼 이어지면 기다리기")]
        public float absentToWaiting = 30f;

        [Tooltip("없음이 이만큼 이어지면 쉬기")]
        public float absentToResting = 5 * 60f;

        [Tooltip("없음이 이만큼 이어지면 세션 자동 종료")]
        public float absentToSessionEnd = 30 * 60f;

        [Tooltip("산책을 수락한 뒤, 감지 결과와 상관없이 쉬기를 유지하는 시간")]
        public float acceptedRestHold = 5 * 60f;
    }
}
