using System;
using System.Collections.Generic;

namespace SimpleColony
{
    public interface IProfessionBehavior
    {
        bool CanWork(World world, Node node);
        bool Work(World world, Pawn pawn, Node node, float dt);
    }
}
