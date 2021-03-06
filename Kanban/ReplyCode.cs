﻿namespace Inedo.BuildMasterExtensions.LeanKit.Kanban
{
    internal enum ReplyCode
    {
        NoData = 100,
        DataRetrievalSuccess = 200,
        DataInsertSuccess = 201,
        DataUpdateSuccess = 202,
        DataDeleteSuccess = 203,
        SystemException = 500,
        MinorException = 501,
        UserException = 502,
        FatalException = 503,
        ThrottleWaitResponse = 800,
        WipOverrideCommentRequired = 900,
        ResendingEmailRequired = 902,
        UnauthorizedAccess = 1000
    }
}
