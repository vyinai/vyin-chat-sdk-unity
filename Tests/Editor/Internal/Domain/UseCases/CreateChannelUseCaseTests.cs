// Tests/Editor/Internal/Domain/UseCases/CreateChannelUseCaseTests.cs
// Unit tests for CreateChannelUseCase
// Task 4.1: CreateChannel - Tests

using NUnit.Framework;
using System;
using System.Collections.Generic;
using VyinChatSdk.Internal.Domain.UseCases;
using VyinChatSdk.Tests.Mocks.Data;

namespace VyinChatSdk.Tests.Editor.Internal.Domain.UseCases
{
    [TestFixture]
    public class CreateChannelUseCaseTests
    {
        private MockChannelRepository _mockRepository;
        private CreateChannelUseCase _useCase;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new MockChannelRepository();
            _useCase = new CreateChannelUseCase(_mockRepository);
        }

        [TearDown]
        public void TearDown()
        {
            _mockRepository.Reset();
        }

        #region Success Cases

        [Test]
        public void ExecuteAsync_ValidParams_ReturnsChannel()
        {
            // Arrange
            var createParams = new VcGroupChannelCreateParams
            {
                Name = "Test Channel",
                UserIds = new List<string> { "user1", "user2" }
            };

            // Act
            var result = _useCase.ExecuteAsync(createParams).GetAwaiter().GetResult();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Test Channel", result.Name);
            Assert.AreEqual(1, _mockRepository.OperationHistory.Count);
            Assert.AreEqual("CreateChannel", _mockRepository.OperationHistory[0].operation);
        }

        [Test]
        public void ExecuteAsync_WithOperatorUserIds_IncludesOperators()
        {
            // Arrange
            var createParams = new VcGroupChannelCreateParams
            {
                Name = "Test Channel",
                UserIds = new List<string> { "user1", "user2" },
                OperatorUserIds = new List<string> { "operator1" }
            };

            // Act
            var result = _useCase.ExecuteAsync(createParams).GetAwaiter().GetResult();

            // Assert
            Assert.IsNotNull(result);
            var operation = _mockRepository.OperationHistory[0];
            var params_ = (VcGroupChannelCreateParams)operation.parameters;
            Assert.IsNotNull(params_.OperatorUserIds);
            Assert.AreEqual(1, params_.OperatorUserIds.Count);
            Assert.AreEqual("operator1", params_.OperatorUserIds[0]);
        }

        [Test]
        public void ExecuteAsync_IsDistinct_SetCorrectly()
        {
            // Arrange
            var createParams = new VcGroupChannelCreateParams
            {
                Name = "Distinct Channel",
                UserIds = new List<string> { "user1", "user2" },
                IsDistinct = true
            };

            // Act
            var result = _useCase.ExecuteAsync(createParams).GetAwaiter().GetResult();

            // Assert
            Assert.IsNotNull(result);
            var operation = _mockRepository.OperationHistory[0];
            var params_ = (VcGroupChannelCreateParams)operation.parameters;
            Assert.IsTrue(params_.IsDistinct);
        }

        #endregion

        #region Validation Error Cases

        [Test]
        public void ExecuteAsync_NullParams_ThrowsVcException()
        {
            // Act & Assert
            var ex = Assert.Throws<VcException>(() =>
            {
                _useCase.ExecuteAsync(null).GetAwaiter().GetResult();
            });

            Assert.AreEqual(VcErrorCode.InvalidParameter, ex.ErrorCode);
            Assert.That(ex.Message, Does.Contain("cannot be null"));
        }

        [Test]
        public void ExecuteAsync_NullUserIds_ThrowsVcException()
        {
            // Arrange
            var createParams = new VcGroupChannelCreateParams
            {
                Name = "Test Channel",
                UserIds = null
            };

            // Act & Assert
            var ex = Assert.Throws<VcException>(() =>
            {
                _useCase.ExecuteAsync(createParams).GetAwaiter().GetResult();
            });

            Assert.AreEqual(VcErrorCode.InvalidParameter, ex.ErrorCode);
            Assert.That(ex.Message, Does.Contain("UserIds"));
        }

        [Test]
        public void ExecuteAsync_EmptyUserIds_ThrowsVcException()
        {
            // Arrange
            var createParams = new VcGroupChannelCreateParams
            {
                Name = "Test Channel",
                UserIds = new List<string>()
            };

            // Act & Assert
            var ex = Assert.Throws<VcException>(() =>
            {
                _useCase.ExecuteAsync(createParams).GetAwaiter().GetResult();
            });

            Assert.AreEqual(VcErrorCode.InvalidParameter, ex.ErrorCode);
            Assert.That(ex.Message, Does.Contain("UserIds"));
        }

        #endregion

        #region Error Handling Cases

        [Test]
        public void ExecuteAsync_RepositoryThrowsException_WrapsInVcException()
        {
            // Arrange
            var createParams = new VcGroupChannelCreateParams
            {
                Name = "Test Channel",
                UserIds = new List<string> { "user1", "user2" }
            };
            var innerException = new InvalidOperationException("Repository error");
            _mockRepository.SetExceptionToThrow(innerException);

            // Act & Assert
            var ex = Assert.Throws<VcException>(() =>
            {
                _useCase.ExecuteAsync(createParams).GetAwaiter().GetResult();
            });

            Assert.AreEqual(VcErrorCode.UnknownError, ex.ErrorCode);
            Assert.IsNotNull(ex.InnerException);
            Assert.AreEqual(innerException, ex.InnerException);
        }

        [Test]
        public void ExecuteAsync_RepositoryThrowsVcException_PreservesException()
        {
            // Arrange
            var createParams = new VcGroupChannelCreateParams
            {
                Name = "Test Channel",
                UserIds = new List<string> { "user1", "user2" }
            };
            var vcException = new VcException(VcErrorCode.ErrInvalidSession, "Invalid session key");
            _mockRepository.SetExceptionToThrow(vcException);

            // Act & Assert
            var ex = Assert.Throws<VcException>(() =>
            {
                _useCase.ExecuteAsync(createParams).GetAwaiter().GetResult();
            });

            Assert.AreEqual(VcErrorCode.ErrInvalidSession, ex.ErrorCode);
            Assert.AreEqual(vcException, ex);
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_NullRepository_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CreateChannelUseCase(null));
        }

        #endregion
    }
}
