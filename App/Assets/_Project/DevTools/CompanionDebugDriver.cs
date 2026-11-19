using StudyMate.Core.BreakSuggestion;
using StudyMate.Core.Companion;
using StudyMate.Core.Session;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace StudyMate.DevTools
{
    /// <summary>
    /// 카메라 없이 상태 머신, 세션 시간 계산, 휴식 제안을 시험하는 개발용 컴포넌트.
    /// 빈 GameObject에 붙이고 Play를 누른다. 출시 빌드 전에는 장면에서 뺀다.
    ///
    /// 키: Space 착석/이탈 전환 · B 산책하기 · N 조금 더 · E 세션 종료 · 1/2/3 시간 배속(x1, x10, x60)
    /// 제안 문구가 없을 때 B를 누르면 바로 쉬기로 간다(시험용).
    /// </summary>
    public sealed class CompanionDebugDriver : MonoBehaviour
    {
        [FormerlySerializedAs("rules")]
        [SerializeField] PresenceRules presenceRules = new PresenceRules();
        [SerializeField] BreakRules breakRules = new BreakRules();
        [SerializeField] bool startPresent = true;

        [Tooltip("돌아오기 걷기 애니메이션 대신 기다리는 시간(초)")]
        [SerializeField] float returnWalkSeconds = 3f;

        CompanionStateMachine machine;
        SessionTracker tracker;
        BreakSuggester suggester;

        double simTime;
        float speed = 1f;
        bool simulatedPresent;
        int lastReportedSecond = -1;
        double returningSince;
        GUIStyle boxStyle;

        void Awake()
        {
            simulatedPresent = startPresent;
            machine = new CompanionStateMachine(presenceRules);

            // 로그는 가장 먼저 구독해야 상태가 바뀐 순서대로 찍힌다.
            machine.StateChanged += change =>
            {
                Log($"{change.From} → {change.To}");
                if (change.To == CompanionState.Returning) returningSince = change.Time;
            };
            machine.SessionStarted += _ => Log("세션 시작");
            machine.RestStarted += (reason, _) => Log($"쉬기 시작 ({reason})");

            // 생성 순서가 중요하다: tracker가 suggester보다 먼저 상태 변화를 받아야 한다.
            tracker = new SessionTracker(machine);
            suggester = new BreakSuggester(machine, tracker, breakRules);

            suggester.Shown += () => Log("휴식 제안: \"산책할 시간이에요. 산책하시겠어요?\"");
            suggester.Hidden += result => Log($"휴식 제안 닫힘 ({result})");
            tracker.SessionCompleted += record => Log(
                $"세션 기록 — 공부한 날 {record.Day:yyyy-MM-dd}, 함께한 시간 {Clock(record.togetherSeconds)}, " +
                $"가장 길게 {Clock(record.longestStretchSeconds)}, " +
                $"쉼 {record.restsBreakAccepted + record.restsLongAbsence}회(제안 {record.restsBreakAccepted}, 이탈 {record.restsLongAbsence}), " +
                $"종료 {record.endReason}");
        }

        void Update()
        {
            HandleKeys();
            simTime += Time.unscaledDeltaTime * speed;

            // 기획서 8.4: 감지는 초당 1회. 배속과 상관없이 시뮬레이션 시간 1초마다 한 번 보고한다.
            int second = (int)simTime;
            while (lastReportedSecond < second)
            {
                lastReportedSecond++;
                machine.ReportPresence(simulatedPresent, lastReportedSecond);
                suggester.Tick(lastReportedSecond);
            }
            machine.Tick(simTime);
            suggester.Tick(simTime);

            // 걷기 애니메이션이 아직 없으니, 정해진 시간이 지나면 다 걸었다고 알린다.
            if (machine.State == CompanionState.Returning && simTime - returningSince >= returnWalkSeconds)
                machine.CompleteReturn(simTime);
        }

        void HandleKeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.spaceKey.wasPressedThisFrame) simulatedPresent = !simulatedPresent;
            if (keyboard.bKey.wasPressedThisFrame)
            {
                if (suggester.IsShowing) suggester.Accept(simTime);
                else machine.AcceptBreak(simTime);
            }
            if (keyboard.nKey.wasPressedThisFrame) suggester.Snooze(simTime);
            if (keyboard.eKey.wasPressedThisFrame) machine.EndSession(simTime);
            if (keyboard.digit1Key.wasPressedThisFrame) speed = 1f;
            if (keyboard.digit2Key.wasPressedThisFrame) speed = 10f;
            if (keyboard.digit3Key.wasPressedThisFrame) speed = 60f;
        }

        void OnGUI()
        {
            boxStyle ??= new GUIStyle(GUI.skin.box)
            {
                fontSize = 22,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(16, 16, 12, 12),
            };

            string prompt = suggester.IsShowing
                ? $"BREAK PROMPT  [B] walk  [N] a bit more  ({suggester.PromptSecondsLeft(simTime):0}s)"
                : $"Next prompt at continuous {Clock(suggester.NextAtContinuous)}";

            string text =
                $"State       {machine.State}\n" +
                $"Presence    {(simulatedPresent ? "PRESENT" : "ABSENT")}  ({machine.PresenceDuration(simTime):0}s)\n" +
                $"Time        {Clock(simTime)}   x{speed:0}\n\n" +
                $"Together    {Clock(tracker.TogetherSeconds(simTime))}   pending {Clock(tracker.PendingSeconds(simTime))}\n" +
                $"Continuous  {Clock(tracker.ContinuousSeconds(simTime))}   longest {Clock(tracker.LongestStretchSeconds(simTime))}\n" +
                $"Rests       accepted {tracker.RestsBreakAccepted}   long absence {tracker.RestsLongAbsence}\n" +
                $"{prompt}\n\n" +
                "[Space] seat/leave  [B] walk  [N] a bit more  [E] end\n" +
                "[1] x1   [2] x10   [3] x60";

            GUI.Box(new Rect(16, 16, 760, 330), text, boxStyle);
        }

        void Log(string message) => Debug.Log($"[Companion] {Clock(simTime)}  {message}");

        static string Clock(double seconds)
        {
            int s = (int)seconds;
            return s >= 3600 ? $"{s / 3600}:{s / 60 % 60:00}:{s % 60:00}" : $"{s / 60:00}:{s % 60:00}";
        }
    }
}
