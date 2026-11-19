using System;

namespace StudyMate.Core.Session
{
    /// <summary>
    /// 하루의 경계 (기획서 8.8). 하루는 새벽 4시에 바뀐다. 밤늦게 공부한 시간은 전날에 들어간다.
    /// 세션은 시작한 시각을 기준으로 하루에 속한다. 새벽 4시를 넘겨도 나누지 않는다.
    /// </summary>
    public static class StudyDay
    {
        public const int StartHour = 4;

        /// <summary>현지 시각이 속한 "공부한 날".</summary>
        public static DateTime Of(DateTime local) => local.AddHours(-StartHour).Date;
    }
}
