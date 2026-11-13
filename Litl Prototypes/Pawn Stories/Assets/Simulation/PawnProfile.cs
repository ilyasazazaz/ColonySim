using System.Collections.Generic;

namespace PawnStories
{
    public enum Motive { Connection, Security, Discovery, Craft }
    public enum Skill { Social, Care, Survey, Repair }
    public enum Topic { People, Home, Frontier, Tools, Music, Food }

    public sealed class PawnProfile
    {
        public int Id { get; }
        public string Name { get; }
        public int Age { get; }
        public string Origin { get; }
        public string Background { get; }
        public string Memory { get; }
        public Motive Value { get; }
        public Topic Interest { get; }
        public Topic Dislike { get; }
        public int Sociability { get; }
        public int Boldness { get; }
        public int Persistence { get; }
        public IReadOnlyList<int> Skills { get; }

        internal PawnProfile(int id, string name, int age, string origin, string background,
            string memory, Motive value, Topic interest, Topic dislike,
            int sociability, int boldness, int persistence, int[] skills)
        {
            Id = id;
            Name = name;
            Age = age;
            Origin = origin;
            Background = background;
            Memory = memory;
            Value = value;
            Interest = interest;
            Dislike = dislike;
            Sociability = sociability;
            Boldness = boldness;
            Persistence = persistence;
            Skills = System.Array.AsReadOnly(skills);
        }
    }
}
