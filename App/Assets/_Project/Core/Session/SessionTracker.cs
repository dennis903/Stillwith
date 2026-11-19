using System;
using System.Collections.Generic;
using StudyMate.Core.Companion;

namespace StudyMate.Core.Session
{
    /// <summary>
    /// 세션 동안 상태가 바뀐 기록만 모아 두고, 시간은 필요할 때 계산한다. (기획서 8.8, 8.9)
    ///
    /// - 함께한 시간: 착석 시간 + 보류 구간. 보류 구간(기다리기)은 다시 함께하기로 돌아오면 들어가고,
    ///   쉬기로 넘어가면 통째로 빠진다.
    /// - 연속 시간: 착석 시간만 쌓인다. 기다리기 동안은 멈추고(0이 되지 않음), 쉬기에 들어갈 때만 0이 된다.
    /// - 착석 시간 = 함께하기 + 돌아오기 상태에 있던 시간.
    /// </summary>
    public sealed class SessionTracker
    {
        struct Segment
        {
            public CompanionState State;
            public double Start;
        }

        readonly Func<DateTime> localNow;
        readonly List<Segment> segments = new List<Segment>();
        double sessionStart;
        DateTime sessionStartLocal;

        public SessionTracker(CompanionStateMachine machine, Func<DateTime> localNow = null)
        {
            if (machine == null) throw new ArgumentNullException(nameof(machine));
            this.localNow = localNow ?? (() => DateTime.Now);

            machine.StateChanged += OnStateChanged;
            machine.RestStarted += OnRestStarted;
            machine.SessionEnded += OnSessionEnded;
        }

        public bool IsActive { get; private set; }
        public int RestsBreakAccepted { get; private set; }
        public int RestsLongAbsence { get; private set; }

        /// <summary>세션이 끝나 기록 한 건이 만들어졌다.</summary>
        public event Action<SessionRecord> SessionCompleted;

        /// <summary>지금까지 확정된 함께한 시간(초). 진행 중인 기다리기는 아직 넣지 않는다.</summary>
        public double TogetherSeconds(double now)
        {
            double total = 0;
            for (int i = 0; i < segments.Count; i++)
            {
                double duration = End(i, now) - segments[i].Start;
                switch (segments[i].State)
                {
                    case CompanionState.Together:
                    case CompanionState.Returning:
                        total += duration;
                        break;
                    case CompanionState.Waiting:
                        if (i + 1 < segments.Count && segments[i + 1].State == CompanionState.Together)
                            total += duration;
                        break;
                }
            }
            return total;
        }

        /// <summary>진행 중인 보류 구간(초). 돌아오면 함께한 시간에 들어가고, 쉬기로 가면 빠진다.</summary>
        public double PendingSeconds(double now)
        {
            if (!IsActive || segments.Count == 0) return 0;
            var last = segments[segments.Count - 1];
            return last.State == CompanionState.Waiting ? now - last.Start : 0;
        }

        /// <summary>마지막 쉬기 이후 쌓인 연속 시간(초). 휴식 제안의 기준이다.</summary>
        public double ContinuousSeconds(double now)
        {
            double run = 0;
            for (int i = 0; i < segments.Count; i++)
            {
                switch (segments[i].State)
                {
                    case CompanionState.Resting:
                        run = 0;
                        break;
                    case CompanionState.Together:
                    case CompanionState.Returning:
                        run += End(i, now) - segments[i].Start;
                        break;
                }
            }
            return run;
        }

        /// <summary>이 세션에서 가장 길게 이어진 연속 시간(초). 기록에만 남기고 요약 카드에는 넣지 않는다.</summary>
        public double LongestStretchSeconds(double now)
        {
            double run = 0, best = 0;
            for (int i = 0; i < segments.Count; i++)
            {
                switch (segments[i].State)
                {
                    case CompanionState.Resting:
                        best = Math.Max(best, run);
                        run = 0;
                        break;
                    case CompanionState.Together:
                    case CompanionState.Returning:
                        run += End(i, now) - segments[i].Start;
                        break;
                }
            }
            return Math.Max(best, run);
        }

        double End(int index, double now) =>
            index + 1 < segments.Count ? segments[index + 1].Start : now;

        void OnStateChanged(StateChange change)
        {
            if (change.From == CompanionState.Idle)
            {
                segments.Clear();
                RestsBreakAccepted = 0;
                RestsLongAbsence = 0;
                sessionStart = change.Time;
                sessionStartLocal = localNow();
                IsActive = true;
            }

            // Idle도 기록한다. 마지막 구간이 어디서 끝났는지 표시하는 역할이다.
            if (IsActive)
                segments.Add(new Segment { State = change.To, Start = change.Time });
        }

        void OnRestStarted(RestReason reason, double time)
        {
            if (reason == RestReason.BreakAccepted) RestsBreakAccepted++;
            else RestsLongAbsence++;
        }

        void OnSessionEnded(SessionEndReason reason, double time)
        {
            if (!IsActive) return;
            IsActive = false;

            var record = new SessionRecord
            {
                startedAt = sessionStartLocal,
                endedAt = sessionStartLocal.AddSeconds(time - sessionStart),
                togetherSeconds = TogetherSeconds(time),
                longestStretchSeconds = LongestStretchSeconds(time),
                restsBreakAccepted = RestsBreakAccepted,
                restsLongAbsence = RestsLongAbsence,
                endReason = reason,
            };
            SessionCompleted?.Invoke(record);
        }
    }
}
