using System;
using UnityEngine;

namespace StudyMate.Core.BreakSuggestion
{
    /// <summary>휴식 제안 규칙 (기획서 8.7). 초기값이며 써 보며 조정한다. 단위는 초.</summary>
    [Serializable]
    public sealed class BreakRules
    {
        [Tooltip("휴식 제안을 켜고 끈다 (설정 화면, 기획서 8.12)")]
        public bool enabled = true;

        [Tooltip("연속 시간이 이만큼 넘으면 제안한다. [가정] 50분")]
        public float suggestAfter = 50 * 60f;

        [Tooltip("[조금 더]를 눌렀거나 무응답일 때, 연속 시간이 이만큼 더 쌓이면 다시 제안한다. [가정] 15분")]
        public float snoozeFor = 15 * 60f;

        [Tooltip("응답 없이 착석해 있으면 문구가 이 시간 뒤에 사라진다")]
        public float promptTimeout = 20f;
    }
}
