using System;

namespace StudyMate.Core.Companion
{
    /// <summary>
    /// 감지 결과(있음/없음)와 시간을 받아 캐릭터 상태를 정한다. (기획서 8.3, 8.4)
    ///
    /// Unity에 의존하지 않는 순수 C# 클래스다. 화면이나 카메라 없이 시험할 수 있다.
    /// 시간은 부르는 쪽이 넘긴다. 단위는 초이고, 계속 늘어나기만 해야 한다.
    /// </summary>
    public sealed class CompanionStateMachine
    {
        readonly PresenceRules rules;

        bool hasSignal;
        bool present;
        double presenceSince;   // 지금의 있음/없음이 시작된 시각
        double restHoldUntil;   // 산책 수락 후 쉬기를 유지하는 마지막 시각
        bool waitForAbsence;    // 직접 세션을 끝낸 뒤, 한 번 이탈하기 전에는 새 세션을 시작하지 않음

        public CompanionStateMachine(PresenceRules rules)
        {
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        public CompanionState State { get; private set; } = CompanionState.Idle;
        public bool IsSessionActive => State != CompanionState.Idle;
        public bool IsPresent => present;

        /// <summary>상태가 바뀌었다. (이전 상태, 새 상태)</summary>
        public event Action<CompanionState, CompanionState> StateChanged;
        public event Action SessionStarted;
        public event Action<SessionEndReason> SessionEnded;
        public event Action<RestReason> RestStarted;

        /// <summary>지금의 있음/없음이 이어진 시간(초).</summary>
        public double PresenceDuration(double now) => hasSignal ? now - presenceSince : 0;

        /// <summary>감지 결과를 넘긴다. 기획서 기준으로 초당 1회.</summary>
        public void ReportPresence(bool isPresent, double now)
        {
            if (!hasSignal || isPresent != present)
            {
                hasSignal = true;
                present = isPresent;
                presenceSince = now;
            }
            Evaluate(now);
        }

        /// <summary>새 감지 결과 없이 시간이 흐른 것만 반영한다. 매 프레임 불러도 된다.</summary>
        public void Tick(double now) => Evaluate(now);

        /// <summary>휴식 제안에서 [산책하기]를 눌렀다. (기획서 8.7)</summary>
        public void AcceptBreak(double now)
        {
            if (State == CompanionState.Together || State == CompanionState.Waiting)
                EnterRest(RestReason.BreakAccepted, now);
        }

        /// <summary>돌아오기 걷기가 끝났다. 화면 쪽(애니메이션)에서 알려 준다.</summary>
        public void CompleteReturn()
        {
            if (State == CompanionState.Returning)
                ChangeState(CompanionState.Together);
        }

        /// <summary>"오늘은 여기까지" 버튼.</summary>
        public void EndSession()
        {
            if (!IsSessionActive) return;
            waitForAbsence = true;
            FinishSession(SessionEndReason.UserEnded);
        }

        void Evaluate(double now)
        {
            if (!hasSignal) return;

            // 앱이 잠깐 멈췄다 돌아오면 한 번에 여러 단계를 건너야 할 수 있다.
            // (예: 함께하기 → 기다리기 → 쉬기 → 세션 종료) 더 바뀌지 않을 때까지 반복한다.
            for (int i = 0; i < 5; i++)
            {
                if (!Step(now)) break;
            }
        }

        bool Step(double now)
        {
            double held = now - presenceSince;

            switch (State)
            {
                case CompanionState.Idle:
                    if (waitForAbsence)
                    {
                        if (!present) waitForAbsence = false;
                        return false;
                    }
                    if (present && held >= rules.presentToTogether)
                    {
                        ChangeState(CompanionState.Together);
                        SessionStarted?.Invoke();
                        return true;
                    }
                    return false;

                case CompanionState.Together:
                case CompanionState.Returning:
                    if (!present && held >= rules.absentToWaiting)
                    {
                        ChangeState(CompanionState.Waiting);
                        return true;
                    }
                    return false;

                case CompanionState.Waiting:
                    if (present && held >= rules.presentToTogether)
                    {
                        ChangeState(CompanionState.Together);
                        return true;
                    }
                    if (!present && held >= rules.absentToResting)
                    {
                        EnterRest(RestReason.LongAbsence, now);
                        return true;
                    }
                    return false;

                case CompanionState.Resting:
                    if (!present && held >= rules.absentToSessionEnd)
                    {
                        FinishSession(SessionEndReason.LongAbsence);
                        return true;
                    }
                    if (now < restHoldUntil) return false;

                    // 유지 시간이 끝난 "뒤에" 착석이 10초 이어져야 돌아온다. (기획서 8.7)
                    double presentAfterHold = now - Math.Max(presenceSince, restHoldUntil);
                    if (present && presentAfterHold >= rules.presentToTogether)
                    {
                        ChangeState(CompanionState.Returning);
                        return true;
                    }
                    return false;
            }
            return false;
        }

        void EnterRest(RestReason reason, double now)
        {
            restHoldUntil = reason == RestReason.BreakAccepted ? now + rules.acceptedRestHold : now;
            ChangeState(CompanionState.Resting);
            RestStarted?.Invoke(reason);
        }

        void FinishSession(SessionEndReason reason)
        {
            ChangeState(CompanionState.Idle);
            SessionEnded?.Invoke(reason);
        }

        void ChangeState(CompanionState next)
        {
            if (next == State) return;
            var previous = State;
            State = next;
            StateChanged?.Invoke(previous, next);
        }
    }
}
