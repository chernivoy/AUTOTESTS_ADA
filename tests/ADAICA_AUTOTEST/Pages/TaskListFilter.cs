namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Кнопки-фільтри над списком «Meine Aufgaben» у навігаторі Dashboard, у тому порядку, у якому їх показує ADAICA.
/// </summary>
public enum TaskListFilter
{
    /// <summary>«Ungelesen»: завдання, яке користувач ще не прочитав.</summary>
    Unread,

    /// <summary>«Akzeptieren»: прочитане завдання, яке користувач ще не прийняв і не відхилив.</summary>
    Waiting,

    /// <summary>«Angenommen»: прочитане прийняте завдання.</summary>
    Accepted,

    /// <summary>«Abgelehnt»: прочитане відхилене завдання.</summary>
    Refused,

    /// <summary>«Erledigt»: заповнено «Erledigt am».</summary>
    Done,

    /// <summary>«Unerledigt»: «Erledigt am» порожнє.</summary>
    Undone,

    /// <summary>«Offen»: «Geschlossen am» порожнє.</summary>
    Open,

    /// <summary>«Geschlossen»: заповнено «Geschlossen am».</summary>
    Closed,

    /// <summary>«Überfällig»: «Fällig am» минуло, а завдання не виконано.</summary>
    Overdue,

    /// <summary>«Aktuell»: будь-яке завдання, крім запланованого.</summary>
    Actual,

    /// <summary>«Geplant»: наступне завдання серії, час якого ще не настав.</summary>
    Planned,
}

public static class TaskListFilters
{
    /// <summary>Підпис кнопки в ADAICA.</summary>
    public static string Label(this TaskListFilter filter) => filter switch
    {
        TaskListFilter.Unread => "Ungelesen",
        TaskListFilter.Waiting => "Akzeptieren",
        TaskListFilter.Accepted => "Angenommen",
        TaskListFilter.Refused => "Abgelehnt",
        TaskListFilter.Done => "Erledigt",
        TaskListFilter.Undone => "Unerledigt",
        TaskListFilter.Open => "Offen",
        TaskListFilter.Closed => "Geschlossen",
        TaskListFilter.Overdue => "Überfällig",
        TaskListFilter.Actual => "Aktuell",
        TaskListFilter.Planned => "Geplant",
        _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null),
    };

    internal static string AutomationId(this TaskListFilter filter) => filter switch
    {
        TaskListFilter.Unread => "tbtnUnreadTasks",
        TaskListFilter.Waiting => "tbtnWaitingTasks",
        TaskListFilter.Accepted => "tbtnAcceptedTasks",
        TaskListFilter.Refused => "tbtnRefusedTasks",
        TaskListFilter.Done => "tbtnDoneTasks",
        TaskListFilter.Undone => "tbtnUndoneTasks",
        TaskListFilter.Open => "tbtnOpenTasks",
        TaskListFilter.Closed => "tbtnClosedTasks",
        TaskListFilter.Overdue => "tbtnOverdueTask",
        TaskListFilter.Actual => "tbtnActualTask",
        TaskListFilter.Planned => "tbtnPlannedTask",
        _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null),
    };
}
