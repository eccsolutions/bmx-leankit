using System;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;

namespace Inedo.BuildMasterExtensions.LeanKit.Kanban
{
    [Serializable]
    internal sealed class KanbanIssue : Issue
    {
        public KanbanIssue(int id, string status, string title, string description, string release, bool isClosed)
            : base(id.ToString(), status, title, description, release)
        {
            this.IsClosed = isClosed;
        }

        public bool IsClosed { get; private set; }
    }
}
