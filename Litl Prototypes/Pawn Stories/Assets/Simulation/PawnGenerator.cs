using System;

namespace PawnStories
{
    public sealed class PawnGenerator
    {
        // Background supplies related facts together: a memory, a skill baseline and a likely value.
        sealed class Background
        {
            public readonly string Name;
            public readonly string Memory;
            public readonly Motive Motive;
            public readonly Skill Skill;
            public readonly Topic Topic;

            public Background(string name, string memory, Motive motive, Skill skill, Topic topic)
            {
                Name = name;
                Memory = memory;
                Motive = motive;
                Skill = skill;
                Topic = topic;
            }
        }

        static readonly Background[] Backgrounds =
        {
            new Background("Посредничество", "Помирились две семьи после нескольких недель переговоров.", Motive.Connection, Skill.Social, Topic.People),
            new Background("Уход за ранеными", "После обвала пришлось держать лазарет открытым всю ночь.", Motive.Security, Skill.Care, Topic.Home),
            new Background("Полевые экспедиции", "Однажды удалось нанести на карту обход затопленного ущелья.", Motive.Discovery, Skill.Survey, Topic.Frontier),
            new Background("Ремонт в мастерской", "Первый самостоятельно собранный насос спас общий запас воды.", Motive.Craft, Skill.Repair, Topic.Tools)
        };
        static readonly string[] Names = { "Ада", "Илья", "Рен", "Мира", "Лев", "Тая", "Саша", "Ника", "Феликс", "Яна", "Юн", "Оля" };

        public PawnProfile Generate(int seed, int id)
        {
            var random = new Random(unchecked(seed * 486187739 + id * 16777619));
            int origin = random.Next(2);
            // Port and ridge settlements have different distributions but share possible histories.
            int[] weights = origin == 0 ? new[] { 40, 15, 10, 35 } : new[] { 10, 35, 40, 15 };
            int roll = random.Next(100), index = 0;
            while (roll >= weights[index]) roll -= weights[index++];
            Background background = Backgrounds[index];
            int age = random.Next(21, 66);
            int[] skills = new int[4];
            for (int i = 0; i < skills.Length; i++) skills[i] = random.Next(1, 6);
            skills[(int)background.Skill] = random.Next(6, age >= 30 ? 11 : 9);
            Motive value = random.Next(100) < 65 ? background.Motive : (Motive)random.Next(4);
            Topic interest = random.Next(100) < 60 ? background.Topic : (Topic)random.Next(6);
            int dislike = random.Next(5);
            if (dislike >= (int)interest) dislike++;
            int name = (int)(((uint)seed + (ulong)(uint)id) % (uint)Names.Length);
            return new PawnProfile(id, Names[name], age, origin == 0 ? "Речной порт" : "Горный посёлок",
                background.Name, background.Memory, value, interest, (Topic)dislike,
                Temperament(random), Temperament(random), Temperament(random), skills);
        }

        static int Temperament(Random random) => (random.Next(101) + random.Next(101)) / 2;
    }
}
