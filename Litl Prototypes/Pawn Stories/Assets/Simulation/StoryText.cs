namespace PawnStories
{
    public static class StoryText
    {
        static readonly string[] Motives = { "Общение", "Безопасность", "Познание", "Мастерство" };
        static readonly string[] Goals = { "Найти своё место среди людей", "Почувствовать себя в безопасности", "Узнать новое об этом месте", "Отточить навыки" };
        static readonly string[] Actions = { "Подготовиться к знакомству", "Устроить личный запас", "Разобрать полевые записи", "Поупражняться в ремесле" };
        static readonly string[] Skills = { "Общение", "Уход", "Разведка", "Ремонт" };
        static readonly string[] Topics = { "Люди", "Дом", "Окрестности", "Инструменты", "Музыка", "Еда" };

        public static string MotiveName(Motive motive) => Motives[(int)motive];
        public static string Goal(Motive motive) => Goals[(int)motive];
        public static string PersonalAction(Motive motive) => Actions[(int)motive];
        public static string SkillName(Skill skill) => Skills[(int)skill];
        public static string TopicName(Topic topic) => Topics[(int)topic];
        public static string Intention(Decision decision)
        {
            switch (decision.Kind)
            {
                case DecisionKind.Sleep: return "Спать";
                case DecisionKind.Eat: return "Поесть";
                case DecisionKind.Rest: return "Восстановить силы";
                case DecisionKind.Ticket: return decision.Ticket.Title;
                default: return PersonalAction(decision.Goal);
            }
        }
    }
}
