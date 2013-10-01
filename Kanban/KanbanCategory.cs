using System;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;

namespace Inedo.BuildMasterExtensions.LeanKit.Kanban
{
    [Serializable]
    internal sealed class KanbanCategory : IssueTrackerCategory
    {
        public KanbanCategory(int id, string name)
            : base(id.ToString(), name, null)
        {
        }

        public new int CategoryId
        {
            get { return int.Parse(base.CategoryId); }
        }
    }
}
