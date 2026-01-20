using System;
using System.Collections.Generic;

namespace VyinChatSdk.Internal.Data.Mappers
{
    internal static class ErrorCodeMapper
    {
        // Legacy error code mapping based on v0 -> v1 table (see Error Handling skill.md).
        // Ambiguous old codes use generic fallbacks to avoid UnknownError.
        private static readonly Dictionary<int, VcErrorCode> _legacyMapping = new Dictionary<int, VcErrorCode>
        {
            { 400, VcErrorCode.ErrBadRequest }, // ambiguous (ErrBadRequest/ErrInvalidArgument)
            { 401, VcErrorCode.ErrUnauthorized }, // ambiguous (session/auth variants)
            { 409, VcErrorCode.ErrResourceAlreadyExists }, // ambiguous (exists variants)
            { 400301, VcErrorCode.ErrInvalidValue }, // ambiguous (user/message variants)
            { 200, VcErrorCode.ErrZaloUnauthorized }, // 200 -> 914151
            { 403, VcErrorCode.ErrForbidden }, // 403 -> 403000
            { 404, VcErrorCode.ErrNotFound }, // 404 -> 404000
            { 412, VcErrorCode.ErrPreconditionFailed }, // 412 -> 412000
            { 422, VcErrorCode.ErrOrgNotAwsBindable }, // 422 -> 730007
            { 100000, VcErrorCode.ErrParameterDecode }, // 100000 -> 400011
            { 106006, VcErrorCode.ErrInvalidValueBotProfileLanguage }, // 106006 -> 638000
            { 107101, VcErrorCode.ErrInvalidAppID }, // 107101 -> 638001
            { 183101, VcErrorCode.ErrContactUsSubjectTooLong }, // 183101 -> 183000
            { 183102, VcErrorCode.ErrContactUsDescriptionTooLong }, // 183102 -> 183001
            { 183103, VcErrorCode.ErrContactUsRequiredFailed }, // 183103 -> 183002
            { 305400, VcErrorCode.ErrInvalidLicenseID }, // 305400 -> 305000
            { 305401, VcErrorCode.ErrLicenseNotFound }, // 305401 -> 305001
            { 400101, VcErrorCode.ErrUnexpectedParameterTypeNumber }, // 400101 -> 400002
            { 400108, VcErrorCode.ErrUnauthorized }, // 400108 -> 400003
            { 400110, VcErrorCode.ErrInputExceedLimit }, // 400110 -> 400004
            { 400111, VcErrorCode.ErrInvalidValue }, // 400111 -> 400005
            { 400112, VcErrorCode.ErrNotFoundInDatabase }, // 400112 -> 400006
            { 400113, VcErrorCode.ErrNameRequired }, // 400113 -> 357007
            { 400114, VcErrorCode.ErrEmailInvalid }, // 400114 -> 357008
            { 400115, VcErrorCode.ErrPhoneInvalid }, // 400115 -> 357009
            { 400116, VcErrorCode.ErrAwsResolveCustomerFailed }, // 400116 -> 357010
            { 400121, VcErrorCode.ErrWebhookConfigEventsInvalid }, // 400121 -> 516001
            { 400202, VcErrorCode.ErrResourceAlreadyExists }, // 400202 -> 400007
            { 400204, VcErrorCode.ErrZendeskConversationNotFound }, // 400204 -> 914201
            { 400300, VcErrorCode.ErrDeactivatedUserNotAccessible }, // 400300 -> 279002
            { 400303, VcErrorCode.ErrInvalidSessionKeyValue }, // 400303 -> 400008
            { 400304, VcErrorCode.ErrApplicationNotFound }, // 400304 -> 638003
            { 400400, VcErrorCode.ErrTagNameRequired }, // 400400 -> 762001
            { 400401, VcErrorCode.ErrInvalidAPIToken }, // 400401 -> 400009
            { 400403, VcErrorCode.ErrJSONParser }, // 400403 -> 400010
            { 400700, VcErrorCode.ErrInBlockRelation }, // 400700 -> 307016
            { 400701, VcErrorCode.ErrBlockAndInvitedNotAllowed }, // 400701 -> 307017
            { 400901, VcErrorCode.ErrChannelUserLimit }, // 400901 -> 279005
            { 403202, VcErrorCode.ErrForbiddenFeatureGroupPermission }, // 403202 -> 357005
            { 403203, VcErrorCode.ErrForbiddenOutOfQuota }, // 403203 -> 357006
            { 403901, VcErrorCode.ErrNotSupportDefaultPlan }, // 403901 -> 357025
            { 404101, VcErrorCode.ErrRouterNotFound }, // 404101 -> 404001
            { 404102, VcErrorCode.ErrWebhookConfigEventsNotFound }, // 404102 -> 516000
            { 404103, VcErrorCode.ErrFileNotFound }, // 404103 -> 404002
            { 409001, VcErrorCode.ErrAwsPaymentMethodExisted }, // 409001 -> 357001
            { 409100, VcErrorCode.ErrNotAbleToCancelDowngradeRequest }, // 409100 -> 357002
            { 409101, VcErrorCode.ErrNotAbleToCancelUnsubscribeRequest }, // 409101 -> 357003
            { 409102, VcErrorCode.ErrOrgInDowngradeCoolingOffPeriod }, // 409102 -> 730000
            { 409103, VcErrorCode.ErrOrgInUnsubscribeCoolingOffPeriod }, // 409103 -> 730001
            { 409104, VcErrorCode.ErrOrgNoCurrentSubscription }, // 409104 -> 730002
            { 409105, VcErrorCode.ErrUpcomingSubscriptionNotActive }, // 409105 -> 357004
            { 429101, VcErrorCode.ErrServerBusy }, // 429101 -> 429000
            { 500901, VcErrorCode.ErrInternal }, // 500901 -> 500000
            { 645004, VcErrorCode.ErrBotKnowledgeCategoryExisted }, // 645004 -> 645000
            { 645005, VcErrorCode.ErrInvalidBotKnowledgeCategory }, // 645005 -> 645001
            { 645006, VcErrorCode.ErrBotEngineProtocolKnowledgeSourceNotSupported }, // 645006 -> 645002
            { 645101, VcErrorCode.Err3rdPartyServiceError }, // 645101 -> 645004
            { 645102, VcErrorCode.Err3rdPartyServiceErrorUnknownEvent }, // 645102 -> 645005
            { 645103, VcErrorCode.ErrContextCancelled }, // 645103 -> 645006
            { 645104, VcErrorCode.Err3rdPartyServiceErrorContextDeadlineExceeded }, // 645104 -> 645007
            { 645412, VcErrorCode.ErrBotEnginePreconditionFailed }, // 645412 -> 645008
            { 645413, VcErrorCode.ErrBotIsInUse }, // 645413 -> 645009
            { 900022, VcErrorCode.ErrNotAnOwner }, // 900022 -> 307000
            { 900023, VcErrorCode.ErrSendNotAllowed }, // 900023 -> 307001
            { 900041, VcErrorCode.ErrUserIsMuted }, // 900041 -> 307002
            { 900500, VcErrorCode.ErrChannelNotFound }, // 900500 -> 279004
            { 925160, VcErrorCode.ErrIntegrationExists }, // 925160 -> 914002
            { 925161, VcErrorCode.ErrDiscordIntegrationNotFound }, // 925161 -> 914051
            { 925162, VcErrorCode.ErrDiscordIntegrationInactive }, // 925162 -> 914052
            { 925163, VcErrorCode.ErrDiscordBotTokenRequired }, // 925163 -> 914053
            { 925164, VcErrorCode.ErrDiscordGuildChannelNotSupported }, // 925164 -> 914054
            { 925165, VcErrorCode.ErrDiscordBotPermissionDenied }, // 925165 -> 914055
            { 925166, VcErrorCode.ErrDiscordBotChannelPermissionDenied }, // 925166 -> 914056
            { 925167, VcErrorCode.ErrDiscordInvalidState }, // 925167 -> 914057
            { 925168, VcErrorCode.ErrDiscordStateExpired }, // 925168 -> 914058
            { 925169, VcErrorCode.ErrDiscordTokenExchangeFailed }, // 925169 -> 914059
            { 925170, VcErrorCode.ErrDiscordUserInfoFailed }, // 925170 -> 914060
            { 925171, VcErrorCode.ErrDiscordUnauthorized }, // 925171 -> 914061
            { 925172, VcErrorCode.ErrDiscordForbidden }, // 925172 -> 914062
            { 925173, VcErrorCode.ErrDiscordNotFound }, // 925173 -> 914063
            { 925174, VcErrorCode.ErrDiscordRateLimit }, // 925174 -> 914064
            { 925175, VcErrorCode.ErrDiscordAPIError }, // 925175 -> 914065
            { 925176, VcErrorCode.ErrDiscordFileTooLarge }, // 925176 -> 914066
            { 925177, VcErrorCode.ErrDiscordDisconnectAuthFailed }, // 925177 -> 914067
            { 925178, VcErrorCode.ErrDiscordDisconnectInactive }, // 925178 -> 914068
            { 925179, VcErrorCode.ErrDiscordDisconnectNotFound }, // 925179 -> 914069
            { 925180, VcErrorCode.ErrDiscordDisconnectUnauthorized }, // 925180 -> 914070
            { 925181, VcErrorCode.ErrFileIsNotReady }, // 925181 -> 307003
            { 925182, VcErrorCode.ErrSocialAlreadyConnected }, // 925182 -> 914001
            { 925183, VcErrorCode.ErrTelegramIntegrationExpired }, // 925183 -> 914101
            { 925184, VcErrorCode.ErrTelegramFloodWait }, // 925184 -> 914102
            { 7050004, VcErrorCode.ErrDatabase }, // 7050004 -> 500001
            { 7050010, VcErrorCode.ErrDuplicateRecord }, // 7050010 -> 645003
            { 7400201, VcErrorCode.ErrInvitationNotFound }, // 7400201 -> 279001
            { 7400305, VcErrorCode.ErrOrgNotFound }, // 7400305 -> 730003
            { 7400306, VcErrorCode.ErrOrgExceedsLimit }, // 7400306 -> 730004
            { 7400307, VcErrorCode.ErrMemIsDeactivated }, // 7400307 -> 279003
            { 7400308, VcErrorCode.ErrUserNotOrgMember }, // 7400308 -> 730005
            { 7400800, VcErrorCode.ErrOrgMemberInvitationNotFound }, // 7400800 -> 730008
            { 7403101, VcErrorCode.ErrAPPPermissionDeny }, // 7403101 -> 638004
            { 7500001, VcErrorCode.ErrOtherService }, // 7500001 -> 500002
            { 7500003, VcErrorCode.ErrMarshalFailed }, // 7500003 -> 500003
            { 7500005, VcErrorCode.ErrHttpRequestTimeout }, // 7500005 -> 500004
            { 7500006, VcErrorCode.ErrNotSupportFeatureGroup }, // 7500006 -> 357000
            { 7900050, VcErrorCode.ErrChannelFreeze }, // 7900050 -> 279000
            { 7900300, VcErrorCode.ErrMessageNotFound }, // 7900300 -> 307004
            { 9251519, VcErrorCode.ErrAnActiveCallExists }, // 9251519 -> 166010
            { 9251520, VcErrorCode.ErrCallerInActiveCall }, // 9251520 -> 166000
            { 9251521, VcErrorCode.ErrCalleeInActiveCall }, // 9251521 -> 166001
            { 9251522, VcErrorCode.ErrMemberNotInDirectCall }, // 9251522 -> 166002
            { 9251523, VcErrorCode.ErrParticipantExistInRoom }, // 9251523 -> 166003
            { 9251524, VcErrorCode.ErrParticipantIsNotModerator }, // 9251524 -> 166004
            { 9251525, VcErrorCode.ErrCallEnded }, // 9251525 -> 166005
            { 9251526, VcErrorCode.ErrParticipantNotInCall }, // 9251526 -> 166006
            { 9251527, VcErrorCode.ErrUserDeviceNotFound }, // 9251527 -> 166007
            { 9251528, VcErrorCode.ErrSourceExist }, // 9251528 -> 645010
            { 9251529, VcErrorCode.ErrDataSourceInvalid }, // 9251529 -> 645011
            { 9251535, VcErrorCode.ErrMaximumQuotaUploadSource }, // 9251535 -> 357011
            { 9251536, VcErrorCode.ErrVideoCallMaximumParticipant }, // 9251536 -> 357012
            { 9251537, VcErrorCode.ErrAudioCallMaximumParticipant }, // 9251537 -> 357013
            { 9251538, VcErrorCode.ErrCallNotInChannel }, // 9251538 -> 166011
            { 9251539, VcErrorCode.ErrNotDirectCall }, // 9251539 -> 166008
            { 9251540, VcErrorCode.ErrUserNotMember }, // 9251540 -> 166009
            { 9251541, VcErrorCode.ErrPollNotFound }, // 9251541 -> 307006
            { 9251542, VcErrorCode.ErrPollOptionNotFound }, // 9251542 -> 307007
            { 9251543, VcErrorCode.ErrMultipleVotePollNotFound }, // 9251543 -> 307008
            { 9251544, VcErrorCode.ErrPollAlreadyExistsForMessage }, // 9251544 -> 307009
            { 9251545, VcErrorCode.ErrPollNotLinkedToMessage }, // 9251545 -> 307010
            { 9251546, VcErrorCode.ErrPollNotOpen }, // 9251546 -> 307011
            { 9251547, VcErrorCode.ErrBlockProfanityMessage }, // 9251547 -> 307013
            { 9251548, VcErrorCode.ErrDomainFilter }, // 9251548 -> 307014
            { 9251549, VcErrorCode.ErrPollHasClosedOrRemoved }, // 9251549 -> 307012
            { 9251550, VcErrorCode.ErrOptionsExceedMax }, // 9251550 -> 307015
            { 9251551, VcErrorCode.ErrQuotaUploadTrafficExceeded }, // 9251551 -> 357014
            { 9251552, VcErrorCode.ErrQuotaFileStorageExceeded }, // 9251552 -> 357015
            { 9251553, VcErrorCode.ErrQuotaBotInterfaceExceeded }, // 9251553 -> 357016
            { 9251554, VcErrorCode.ErrQuotaAutoTranslationExceeded }, // 9251554 -> 357017
            { 9251555, VcErrorCode.ErrQuotaAnnouncementExceeded }, // 9251555 -> 357018
            { 9251556, VcErrorCode.ErrQuotaMessageSearchIndexExceeded }, // 9251556 -> 357019
            { 9251557, VcErrorCode.ErrQuotaMessageSearchQueryExceeded }, // 9251557 -> 357020
            { 9251558, VcErrorCode.ErrQuotaAutoThumbnailExceeded }, // 9251558 -> 357021
            { 9251559, VcErrorCode.ErrQuotaMAUSystemLimitExceeded }, // 9251559 -> 357024
            { 9251560, VcErrorCode.ErrNotSelectRolePermission }, // 9251560 -> 730006
            { 9251563, VcErrorCode.ErrQuotaMAUExceeded }, // 9251563 -> 357022
            { 9251564, VcErrorCode.ErrQuotaPCCExceeded }, // 9251564 -> 357023
            { 9300000, VcErrorCode.ErrInvalidAPNSConfig }, // 9300000 -> 348000
            { 9300001, VcErrorCode.ErrInvalidFCMConfig }, // 9300001 -> 348001
        };

        /// <summary>
        /// Maps an integer error code (from JSON body or other source) to VcErrorCode.
        /// Handles legacy code compatibility.
        /// </summary>
        public static VcErrorCode FromApiCode(int code)
        {
            // 1. Check for legacy mapping
            if (_legacyMapping.TryGetValue(code, out var mappedCode))
            {
                return mappedCode;
            }

            // 2. Cast directly if defined in Enum
            if (Enum.IsDefined(typeof(VcErrorCode), code))
            {
                return (VcErrorCode)code;
            }

            // 3. Fallback to Unknown if totally unrecognized
            return VcErrorCode.UnknownError;
        }

        /// <summary>
        /// Maps HTTP status code to VcErrorCode when detailed API error is missing.
        /// </summary>
        public static VcErrorCode FromHttpStatusFallback(int statusCode)
        {
            return statusCode switch
            {
                400 => VcErrorCode.ErrInvalidValue,       // Bad Request
                401 => VcErrorCode.ErrInvalidSession,     // Unauthorized
                403 => VcErrorCode.ErrForbidden,          // Forbidden
                404 => VcErrorCode.ErrNotFound,           // Not Found (generic)
                412 => VcErrorCode.ErrPreconditionFailed, // Precondition Failed
                429 => VcErrorCode.ErrServerBusy,         // Too Many Requests
                500 => VcErrorCode.ErrInternal,           // Internal Server Error
                503 => VcErrorCode.ErrOtherService,       // Service Unavailable
                504 => VcErrorCode.ErrHttpRequestTimeout, // Gateway Timeout
                _ => VcErrorCode.NetworkError             // Default fallback for other HTTP errors
            };
        }

        /// <summary>
        /// Maps WebSocket close code to VcErrorCode.
        /// </summary>
        public static VcErrorCode FromWebSocketCloseCode(ushort closeCode)
        {
            // Standard WebSocket close codes (1000-1015) can be mapped if needed,
            // but usually we care about custom application codes (4000+) if defined.
            // For now, we map standard anomalies to NetworkError or ConnectionClosed.

            return closeCode switch
            {
                1000 => VcErrorCode.WebSocketConnectionClosed, // Normal Closure
                1001 => VcErrorCode.WebSocketConnectionClosed, // Going Away
                1006 => VcErrorCode.WebSocketConnectionFailed, // Abnormal Closure (no close frame)
                1011 => VcErrorCode.ErrInternal,               // Internal Error
                _ => VcErrorCode.UnknownError
            };
        }
    }
}
