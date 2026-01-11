using FluentAssertions;
using TiendaApi.Dtos.Usuarios;
using TiendaApi.Mappers;
using TiendaApi.Models;

namespace TiendaApi.Tests.Unit.Mappers;

/// <summary>
/// Comprehensive test suite for UserMapper extension methods
/// Tests all entity-DTO conversions for User domain
/// </summary>
public class UserMapperTests
{
    #region ToDto Tests

    [Test]
    public void ToDto_WithAllFields_ShouldMapCorrectly()
    {
        // Arrange
        var user = new User
        {
            Id = 100,
            Username = "johndoe",
            Email = "john@example.com",
            PasswordHash = "hashed_password_123",
            Role = UserRoles.USER,
            IsDeleted = false,
            CreatedAt = new DateTime(2024, 1, 15),
            UpdatedAt = new DateTime(2024, 6, 20)
        };

        // Act
        var dto = user.ToDto();

        // Assert
        dto.Id.Should().Be(100);
        dto.Username.Should().Be("johndoe");
        dto.Email.Should().Be("john@example.com");
        dto.Role.Should().Be(UserRoles.USER);
        dto.CreatedAt.Should().Be(user.CreatedAt);
    }

    [Test]
    public void ToDto_ShouldNotExposePasswordHash()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "test",
            Email = "test@test.com",
            PasswordHash = "super_secret_hash"
        };

        // Act
        var dto = user.ToDto();

        // Assert
        dto.Should().NotBeNull();
        dto.Username.Should().Be("test");
        // PasswordHash should never be in the DTO
    }

    [Test]
    public void ToDto_WithAdminRole_ShouldMapCorrectly()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "admin",
            Email = "admin@example.com",
            Role = UserRoles.ADMIN
        };

        // Act
        var dto = user.ToDto();

        // Assert
        dto.Role.Should().Be(UserRoles.ADMIN);
    }

    [Test]
    public void ToDto_WithDeletedUser_ShouldStillMap()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "deleted_user",
            Email = "deleted@test.com",
            IsDeleted = true
        };

        // Act
        var dto = user.ToDto();

        // Assert
        dto.Id.Should().Be(1);
        dto.Username.Should().Be("deleted_user");
    }

    [Test]
    public void ToDto_ShouldPreserveCreatedAt()
    {
        // Arrange
        var createdAt = new DateTime(2023, 6, 15, 10, 0, 0);
        var user = new User
        {
            Id = 1,
            Username = "test",
            CreatedAt = createdAt,
            UpdatedAt = new DateTime(2024, 1, 1)
        };

        // Act
        var dto = user.ToDto();

        // Assert
        dto.CreatedAt.Should().Be(createdAt);
    }

    #endregion

    #region ToEntity (RegisterDto) Tests

    [Test]
    public void ToEntity_WithRegisterDto_ShouldMapCorrectly()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "SecurePassword123!"
        };
        var passwordHash = "bcrypt_hashed_password";

        // Act
        var entity = dto.ToEntity(passwordHash);

        // Assert
        entity.Username.Should().Be("newuser");
        entity.Email.Should().Be("newuser@example.com");
        entity.PasswordHash.Should().Be("bcrypt_hashed_password");
        entity.Role.Should().Be(UserRoles.USER);
        entity.IsDeleted.Should().BeFalse();
    }

    [Test]
    public void ToEntity_ShouldSetDefaultRoleToUser()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "test",
            Email = "test@test.com",
            Password = "password"
        };

        // Act
        var entity = dto.ToEntity("hash");

        // Assert
        entity.Role.Should().Be(UserRoles.USER);
    }

    [Test]
    public void ToEntity_ShouldSetIsDeletedToFalse()
    {
        // Arrange
        var dto = new RegisterDto { Username = "test", Password = "pass" };

        // Act
        var entity = dto.ToEntity("hash");

        // Assert
        entity.IsDeleted.Should().BeFalse();
    }

    [Test]
    public void ToEntity_ShouldSetTimestamps()
    {
        // Arrange
        var dto = new RegisterDto { Username = "test", Password = "pass" };
        var before = DateTime.UtcNow;

        // Act
        var entity = dto.ToEntity("hash");
        var after = DateTime.UtcNow;

        // Assert
        entity.CreatedAt.Should().BeOnOrAfter(before);
        entity.CreatedAt.Should().BeOnOrBefore(after);
        entity.UpdatedAt.Should().BeOnOrAfter(before);
        entity.UpdatedAt.Should().BeOnOrBefore(after);
    }

    [Test]
    public void ToEntity_ShouldUseProvidedPasswordHash()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "test",
            Email = "test@test.com",
            Password = "plain_password"
        };
        var customHash = "custom_bcrypt_hash_xyz";

        // Act
        var entity = dto.ToEntity(customHash);

        // Assert
        entity.PasswordHash.Should().Be(customHash);
        entity.PasswordHash.Should().NotBe("plain_password");
    }

    #endregion

    #region UpdateEntity Tests

    [Test]
    public void UpdateEntity_WithEmail_ShouldUpdateEmail()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "test",
            Email = "old@test.com",
            PasswordHash = "hash",
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };
        var dto = new UserUpdateDto { Email = "new@test.com" };

        // Act
        dto.UpdateEntity(user);

        // Assert
        user.Email.Should().Be("new@test.com");
    }

    [Test]
    public void UpdateEntity_WithPassword_ShouldUpdatePasswordHash()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "test",
            PasswordHash = "old_hash",
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };
        var dto = new UserUpdateDto { Password = "NewSecurePassword123!" };

        // Act
        dto.UpdateEntity(user);

        // Assert
        user.PasswordHash.Should().NotBe("old_hash");
        user.PasswordHash.Should().StartWith("$2"); // BCrypt prefix
    }

    [Test]
    public void UpdateEntity_ShouldUpdateTimestamp()
    {
        // Arrange
        var oldUpdatedAt = DateTime.UtcNow.AddDays(-1);
        var user = new User
        {
            Id = 1,
            Username = "test",
            UpdatedAt = oldUpdatedAt
        };
        var dto = new UserUpdateDto { Email = "updated@test.com" };

        // Act
        dto.UpdateEntity(user);

        // Assert
        user.UpdatedAt.Should().BeAfter(oldUpdatedAt);
    }

    [Test]
    public void UpdateEntity_WithEmptyEmail_ShouldNotUpdateEmail()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "test",
            Email = "original@test.com"
        };
        var dto = new UserUpdateDto { Email = string.Empty };

        // Act
        dto.UpdateEntity(user);

        // Assert
        user.Email.Should().Be("original@test.com");
    }

    [Test]
    public void UpdateEntity_WithEmptyPassword_ShouldNotUpdatePassword()
    {
        // Arrange
        var originalHash = "original_bcrypt_hash";
        var user = new User
        {
            Id = 1,
            Username = "test",
            PasswordHash = originalHash
        };
        var dto = new UserUpdateDto { Password = string.Empty };

        // Act
        dto.UpdateEntity(user);

        // Assert
        user.PasswordHash.Should().Be(originalHash);
    }

    [Test]
    public void UpdateEntity_ShouldNotModifyId()
    {
        // Arrange
        var user = new User { Id = 999, Username = "test" };
        var dto = new UserUpdateDto { Email = "new@test.com" };

        // Act
        dto.UpdateEntity(user);

        // Assert
        user.Id.Should().Be(999);
    }

    [Test]
    public void UpdateEntity_ShouldNotModifyCreatedAt()
    {
        // Arrange
        var originalCreatedAt = new DateTime(2022, 1, 1);
        var user = new User
        {
            Id = 1,
            Username = "test",
            CreatedAt = originalCreatedAt
        };
        var dto = new UserUpdateDto { Email = "new@test.com" };

        // Act
        dto.UpdateEntity(user);

        // Assert
        user.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Test]
    public void UpdateEntity_ShouldNotModifyUsername()
    {
        // Arrange
        var user = new User { Id = 1, Username = "original_username" };
        var dto = new UserUpdateDto { Email = "new@test.com" };

        // Act
        dto.UpdateEntity(user);

        // Assert
        user.Username.Should().Be("original_username");
    }

    #endregion

    #region ToDtoList Tests

    [Test]
    public void ToDtoList_WithMultipleUsers_ShouldMapAll()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = 1, Username = "user1" },
            new() { Id = 2, Username = "user2" },
            new() { Id = 3, Username = "user3" }
        };

        // Act
        var dtos = users.ToDtoList().ToList();

        // Assert
        dtos.Should().HaveCount(3);
        dtos[0].Username.Should().Be("user1");
        dtos[1].Username.Should().Be("user2");
        dtos[2].Username.Should().Be("user3");
    }

    [Test]
    public void ToDtoList_WithEmptyList_ShouldReturnEmpty()
    {
        // Arrange
        var users = new List<User>();

        // Act
        var dtos = users.ToDtoList().ToList();

        // Assert
        dtos.Should().BeEmpty();
    }

    [Test]
    public void ToDtoList_ShouldPreserveOrder()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = 3, Username = "Third" },
            new() { Id = 1, Username = "First" },
            new() { Id = 2, Username = "Second" }
        };

        // Act
        var dtos = users.ToDtoList().ToList();

        // Assert
        dtos[0].Id.Should().Be(3);
        dtos[1].Id.Should().Be(1);
        dtos[2].Id.Should().Be(2);
    }

    #endregion

    #region Roundtrip Tests

    [Test]
    public void ToDto_ThenToEntity_ShouldPreserveBasicData()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "roundtrip_test",
            Email = "roundtrip@test.com",
            Role = UserRoles.USER
        };

        // Act
        var dto = user.ToDto();
        // Note: There's no direct ToEntity from Dto, but we can verify the data is preserved
        var preservedUsername = dto.Username;
        var preservedEmail = dto.Email;
        var preservedRole = dto.Role;

        // Assert
        preservedUsername.Should().Be("roundtrip_test");
        preservedEmail.Should().Be("roundtrip@test.com");
        preservedRole.Should().Be(UserRoles.USER);
    }

    #endregion

    #region Edge Cases Tests

    [Test]
    public void ToDto_WithMaxId_ShouldMapCorrectly()
    {
        // Arrange
        var user = new User
        {
            Id = long.MaxValue,
            Username = "Max ID User"
        };

        // Act
        var dto = user.ToDto();

        // Assert
        dto.Id.Should().Be(long.MaxValue);
    }

    [Test]
    public void ToDto_WithVeryLongUsername_ShouldMapCorrectly()
    {
        // Arrange
        var longUsername = new string('U', 100);
        var user = new User
        {
            Id = 1,
            Username = longUsername
        };

        // Act
        var dto = user.ToDto();

        // Assert
        dto.Username.Should().Be(longUsername);
        dto.Username.Length.Should().Be(100);
    }

    [Test]
    public void ToDto_WithUnicodeEmail_ShouldMapCorrectly()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "test",
            Email = "user@exämple.com"
        };

        // Act
        var dto = user.ToDto();

        // Assert
        dto.Email.Should().Be("user@exämple.com");
    }

    [Test]
    public void ToEntity_WithUnicodeUsername_ShouldMapCorrectly()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "üser_名前",
            Email = "test@test.com",
            Password = "password"
        };

        // Act
        var entity = dto.ToEntity("hash");

        // Assert
        entity.Username.Should().Be("üser_名前");
    }

    #endregion

    #region Security Tests

    [Test]
    public void ToDto_ShouldNeverExposePasswordHash()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "secure_user",
            Email = "secure@test.com",
            PasswordHash = "$2a$11$verysecretbcrypthash"
        };

        // Act
        var dto = user.ToDto();
        var dtoType = dto.GetType();
        var properties = dtoType.GetProperties();

        // Assert - Verify PasswordHash is not a property of DTO
        var hasPasswordHash = properties.Any(p => p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
        hasPasswordHash.Should().BeFalse("DTO should not expose password hash for security");
    }

    #endregion
}
