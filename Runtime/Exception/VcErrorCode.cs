namespace VyinChatSdk
{
    /// <summary>
    /// Error codes for VyinChat SDK
    /// </summary>
    public enum VcErrorCode
    {
        // Invalid Parameter Errors (400100-400199)
        InvalidParameterValueString = 400100,
        InvalidParameterValueNumber = 400101,
        InvalidParameterValueList = 400102,
        InvalidParameterValueJson = 400103,
        InvalidParameterValueBoolean = 400104,
        InvalidParameterValueRequired = 400105,
        InvalidParameterValuePositive = 400106,
        InvalidParameterValueNegative = 400107,
        NonAuthorized = 400108,
        TokenExpired = 400109,
        InvalidLength = 400110,
        InvalidParameterValue = 400111,
        UnusableCharacterIncluded = 400151,

        // Database Errors (400200-400299)
        NotFoundInDatabase = 400201,
        DuplicatedData = 400202,
        MaxItemExceeded = 400203,

        // User & Session Errors (400300-400399)
        UserDeactivated = 400300,
        UserNotExist = 400301,
        InvalidAccessToken = 400302,
        InvalidSessionKey = 400303,
        ApplicationNotFound = 400304,
        SessionKeyExpired = 400309,
        SessionTokenRevoked = 400310,

        // Request Errors (400400-400499)
        InvalidJsonRequestBody = 400403,
        InvalidRequestUrl = 400404,

        // Channel Errors (400900-400999)
        ChannelUserLimit = 400901,

        // Server Errors (500000-599999)
        InternalServerError = 500901,

        // Client SDK Errors (800000-899999)
        Unknown = 800000,
        InvalidInitialization = 800100,
        ConnectionRequired = 800101,
        ConnectionCanceled = 800102,
        InvalidParameter = 800110,
        NetworkError = 800120,
        NetworkRoutingError = 800121,
        MalformedData = 800130,
        MalformedErrorData = 800140,
        WrongChannelType = 800150,
        MarkAsReadRateLimitExceeded = 800160,
        QueryInProgress = 800170,
        AckTimeout = 800180,
        LoginTimeout = 800190,
        WebSocketConnectionClosed = 800200,
        WebSocketConnectionFailed = 800210,
        RequestFailed = 800220,
        FileUploadCancelFailed = 800230,
        FileUploadCanceled = 800240,
        FileUploadTimeout = 800250,
        FileSizeLimitExceeded = 800260,
        Pending = 800400,
        ParsedInvalidAccessToken = 800500,
        SessionKeyRefreshSucceeded = 800501,
        SessionKeyRefreshFailed = 800502,
        CollectionDisposed = 800600,
        DatabaseError = 800700,
        InvalidJson = 800800,

        // Authorization Errors (900000-900999)
        InvalidAuthority = 900100,
        NotAMember = 900020,
        NotOperator = 900800,
        ChannelNotFound = 900500,

        // Application Errors (100000-199999)
        InvalidApplicationId = 107101
    }
}
