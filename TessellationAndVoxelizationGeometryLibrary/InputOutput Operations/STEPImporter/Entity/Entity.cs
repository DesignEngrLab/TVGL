using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL.IOFunctions.Step
{
    /// <summary>
    /// 
    /// </summary>
    public class Entity
    {
        public Entity()
        { }

        internal long Id { get; private set; }
        protected string Type { get; private set; }
        protected string[] Args { get; private set; }
        protected long[][] ArgIDs { get; private set; }
        //public override string ToString()
        //{
        //    return String.Format("<Entity({0}, {1}, {2})>", Id, Type, String.Join(", ", Data.ToArray()));
        //}

        internal static Entity CreateEntity(string type, long id, IList<string> list)
        {
            Entity entity = (ParseHelper.EntityTypes.ContainsKey(type))
                           ? (Entity)Activator.CreateInstance(ParseHelper.EntityTypes[type])
                           : new Entity();
            entity.Id = id;
            entity.Type = type;
            var length = list.Count;
            entity.Args = new string[length];
            entity.ArgIDs = new long[length][];
            for (int i = 0; i < length; i++)
            {
                if (list[i].StartsWith("#")) entity.ArgIDs[i] = new[] { ParseHelper.ParseId(list[i]) };
                else if (list[i].StartsWith("("))
                {
                    int j = 0;
                    while (char.IsWhiteSpace(list[i][j])) j++;
                    if (!list[i][j].Equals('#'))  //the set of pointer IDs
                    {
                        var subList = ParseHelper.ParseList(list[i]);
                        var subIDs = new long[subList.Count];
                        for (j = 0; i < subList.Count; j++)
                            subIDs[j] = ParseHelper.ParseId(subList[j]);
                        entity.ArgIDs[i] = subIDs;
                    }
                    else entity.Args[i] = list[i];
                }
                else entity.Args[i] = list[i];
            }
            return entity;
        }

        public virtual void Init(Dictionary<long, Entity> Data)
        {

        }
    }
}
