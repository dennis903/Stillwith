using StudyMate.Core.Companion;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StudyMate.DevTools
{
    /// <summary>
    /// 카메라 없이 캐릭터 상태 머신을 시험하는 개발용 컴포넌트.
    /// 빈 GameObject에 붙이고 Play를 누른다. 출시 빌드 전에는 장면에서 뺀다.
    ///
    /// 키: Space 착석/이탈 전환 · B 산책 수락 · E 세션 종료 · 1/2/3 시간 배속(x1, x10, x60)
    /// </summary>
    public sealed class CompanionDebugDriver : MonoBehaviour
    {
        [SerializeField] PresenceRules rules = new PresenceRules();
        [SerializeField] bool startPresent = true;

        [Tooltip("돌아오기 걷기 애니메이션 대신 기다리는 시간(초)")]
        [SerializeField] float returnWalkSeconds = 3f;

        CompanionStateMachine machine;
        double simTime;
        float speed = 1f;
        bool simulatedPresent;
        int lastReportedSecond = -1;
        double returningSince;
        GUIStyle boxStyle;

        void Awake()
        {
            simulatedPresent = startPresent;
            machine = new CompanionStateMachine(rules);

            machine.StateChanged += (from, to) =>
            {
                Debug.Log($"[Companion] {Clock(simTime)}  {from} → {to}");
                if (to == CompanionState.Returning) returningSince = simTime;
            };
            machine.SessionStarted += () => Debug.Log($"[Companion] {Clock(simTime)}  세션 시작");
            machine.SessionEnded += reason => Debug.Log($"[Companion] {Clock(simTime)}  세션 종료 ({reason})");
            machine.RestStarted += reason => Debug.Log($"[Companion] {Clock(simTime)}  쉬기 시작 ({reason})");
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
            }
            machine.Tick(simTime);

            // 걷기 애니메이션이 아직 없으니, 정해진 시간이 지나면 다 걸었다고 알린다.
            if (machine.State == CompanionState.Returning && simTime - returningSince >= returnWalkSeconds)
                machine.CompleteReturn();
        }

        void HandleKeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.spaceKey.wasPressedThisFrame) simulatedPresent = !simulatedPresent;
            if (keyboard.bKey.wasPressedThisFrame) machine.AcceptBreak(simTime);
            if (keyboard.eKey.wasPressedThisFrame) machine.EndSession();
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

            string text =
                $"State     {machine.State}\n" +
                $"Presence  {(simulatedPresent ? "PRESENT" : "ABSENT")}  ({machine.PresenceDuration(simTime):0}s)\n" +
                $"Time      {Clock(simTime)}   x{speed:0}\n\n" +
                "[Space] seat/leave   [B] accept break   [E] end session\n" +
                "[1] x1   [2] x10   [3] x60";

            GUI.Box(new Rect(16, 16, 640, 200), text, boxStyle);
        }

        static string Clock(double seconds)
        {
            int s = (int)seconds;
            return $"{s / 60:00}:{s % 60:00}";
        }
    }
}
