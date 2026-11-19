using System;
using StudyMate.Core.Companion;
using StudyMate.Core.Session;

namespace StudyMate.Core.BreakSuggestion
{
    /// <summary>휴식 제안 문구가 사라진 이유.</summary>
    public enum BreakPromptResult
    {
        /// <summary>[산책하기]를 눌렀다.</summary>
        Accepted,
        /// <summary>문구가 떠 있는 동안 30초 넘게 이탈했다. 수락한 것으로 본다.</summary>
        AcceptedByLeaving,
        /// <summary>[조금 더]를 눌렀다.</summary>
        Snoozed,
        /// <summary>응답 없이 착석한 채로 20초가 지났다.</summary>
        TimedOut,
        /// <summary>세션이 끝났거나 제안이 꺼졌다.</summary>
        Cancelled,
    }

    /// <summary>
    /// 연속 시간이 기준을 넘으면 휴식을 제안한다. (기획서 8.7)
    ///
    /// 화면 쪽은 Shown을 받으면 캐릭터가 일어나 기지개를 켜고 문구를 띄우고,
    /// Hidden을 받으면 문구를 내린다. 버튼은 Accept / Snooze로 연결한다.
    /// "30초 넘게 이탈하면 수락"은 상태 머신이 기다리기로 넘어가는 순간(30초)을 받아 처리한다.
    /// </summary>
    public sealed class BreakSuggester
    {
        readonly CompanionStateMachine machine;
        readonly SessionTracker tracker;
        readonly BreakRules rules;

        double nextAtContinuous;
        double shownAt;

        public BreakSuggester(CompanionStateMachine machine, SessionTracker tracker, BreakRules rules)
        {
            this.machine = machine ?? throw new ArgumentNullException(nameof(machine));
            this.tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));

            nextAtContinuous = rules.suggestAfter;
            machine.StateChanged += OnStateChanged;
        }

        public bool IsShowing { get; private set; }

        /// <summary>다음 제안이 나올 연속 시간(초).</summary>
        public double NextAtContinuous => nextAtContinuous;

        /// <summary>문구가 떠 있으면, 자동으로 사라지기까지 남은 시간(초). 이탈 중이면 줄지 않는다.</summary>
        public double PromptSecondsLeft(double now) =>
            IsShowing ? Math.Max(0, rules.promptTimeout - (now - shownAt)) : 0;

        public event Action Shown;
        public event Action<BreakPromptResult> Hidden;

        /// <summary>매 프레임, 상태 머신을 갱신한 다음에 부른다.</summary>
        public void Tick(double now)
        {
            if (!rules.enabled)
            {
                if (IsShowing) Hide(BreakPromptResult.Cancelled);
                return;
            }

            if (IsShowing)
            {
                if (machine.IsPresent && now - shownAt >= rules.promptTimeout)
                {
                    Hide(BreakPromptResult.TimedOut);
                    nextAtContinuous = tracker.ContinuousSeconds(now) + rules.snoozeFor;
                }
                return;
            }

            // 내가 앉아 있을 때만 제안한다. 캐릭터가 아무도 없는 자리를 보고 기지개를 켜지 않도록.
            if (machine.State == CompanionState.Together && machine.IsPresent &&
                tracker.ContinuousSeconds(now) >= nextAtContinuous)
            {
                IsShowing = true;
                shownAt = now;
                Shown?.Invoke();
            }
        }

        /// <summary>[산책하기]</summary>
        public void Accept(double now)
        {
            if (!IsShowing) return;
            Hide(BreakPromptResult.Accepted);
            machine.AcceptBreak(now);
        }

        /// <summary>[조금 더]</summary>
        public void Snooze(double now)
        {
            if (!IsShowing) return;
            Hide(BreakPromptResult.Snoozed);
            nextAtContinuous = tracker.ContinuousSeconds(now) + rules.snoozeFor;
        }

        void OnStateChanged(StateChange change)
        {
            switch (change.To)
            {
                case CompanionState.Waiting when IsShowing:
                    // 문구가 떠 있는 동안 30초 넘게 이탈 → 산책을 수락한 것으로 본다.
                    Hide(BreakPromptResult.AcceptedByLeaving);
                    machine.AcceptBreak(change.Time);
                    break;

                case CompanionState.Resting:
                    // 연속 시간이 0이 되므로 제안 기준도 처음으로 돌린다.
                    if (IsShowing) Hide(BreakPromptResult.Cancelled);
                    nextAtContinuous = rules.suggestAfter;
                    break;

                case CompanionState.Idle:
                    if (IsShowing) Hide(BreakPromptResult.Cancelled);
                    nextAtContinuous = rules.suggestAfter;
                    break;
            }
        }

        void Hide(BreakPromptResult result)
        {
            IsShowing = false;
            Hidden?.Invoke(result);
        }
    }
}
