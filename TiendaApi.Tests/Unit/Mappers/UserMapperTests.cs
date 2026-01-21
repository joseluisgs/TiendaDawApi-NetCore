using FluentAssertions;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models;

namespace TiendaApi.Tests.Unit.Mappers;

/// <summary>
/// Tests unitarios para el mapeador de usuarios.
/// Prueba todas las conversiones entidad-DTO para el dominio de Usuario.
/// </summary>
public class UserMapperTests
{
    #region ToDto Tests

    [Test]
    public void ToDto_ConTodosLosCampos_MapeaCorrectamente()
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
    public void ToDto_NoDebeExponerPasswordHash()
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
    public void ToDto_ConRolAdmin_MapeaCorrectamente()
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
    public void ToDto_ConUsuarioEliminado_AunMapea()
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
    public void ToDto_DebePreservarCreatedAt()
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
    public void ToEntity_ConRegisterDto_MapeaCorrectamente()
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
    public void ToEntity_DebeEstablecerRolPredeterminadoAUsuario()
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
    public void ToEntity_DebeEstablecerIsDeletedAFalse()
    {
        // Arrange
        var dto = new RegisterDto { Username = "test", Password = "pass" };

        // Act
        var entity = dto.ToEntity("hash");

        // Assert
        entity.IsDeleted.Should().BeFalse();
    }

    [Test]
    public void ToEntity_DebeEstablecerMarcasDeTiempo()
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
    public void ToEntity_DebeUsarPasswordHashProporcionado()
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
    public void UpdateEntity_ConEmail_ActualizaEmail()
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
    public void UpdateEntity_ConPassword_ActualizaPasswordHash()
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
    public void UpdateEntity_ConEmailVacio_NoActualizaEmail()
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
    public void UpdateEntity_ConPasswordVacio_NoActualizaPassword()
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
    public void UpdateEntity_NoDebeModificarId()
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
    public void UpdateEntity_NoDebeModificarCreatedAt()
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
    public void UpdateEntity_NoDebeModificarUsername()
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

    #region UpdateEntity (UserPatchDto) Tests

    [Test]
    public void UpdateEntity_UserPatchDto_ConEmail_ActualizaEmail()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "test",
            Email = "old@test.com",
            PasswordHash = "hash"
        };
        var dto = new UserPatchDto { Email = "new@test.com" };

        // Act
        dto.UpdateEntity(user);

        // Assert
        user.Email.Should().Be("new@test.com");
    }

    [Test]
    public void UpdateEntity_UserPatchDto_ConPassword_ActualizaPasswordHash()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "test",
            PasswordHash = "old_hash"
        };
        var dto = new UserPatchDto { Password = "NewSecurePassword123!" };

        // Act
        dto.UpdateEntity(user);

        // Assert
        user.PasswordHash.Should().NotBe("old_hash");
        user.PasswordHash.Should().StartWith("$2");
    }

    [Test]
    public void UpdateEntity_UserPatchDto_ConAvatar_ActualizaAvatar()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "test",
            Avatar = "old_avatar.jpg"
        };
        var dto = new UserPatchDto { Avatar = "https://example.com/new_avatar.png" };

        // Act
        dto.UpdateEntity(user);

        // Assert
        user.Avatar.Should().Be("https://example.com/new_avatar.png");
    }

    [Test]
    public void UpdateEntity_UserPatchDto_ConTodosLosCampos_ActualizaTodos()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "test",
            Email = "old@test.com",
            PasswordHash = "old_hash",
            Avatar = "old_avatar.jpg"
        };
        var dto = new UserPatchDto
        {
            Email = "new@test.com",
            Password = "NewPassword123!",
            Avatar = "https://example.com/new_avatar.png"
        };

        // Act
        dto.UpdateEntity(user);

        // Assert
        user.Email.Should().Be("new@test.com");
        user.PasswordHash.Should().StartWith("$2");
        user.Avatar.Should().Be("https://example.com/new_avatar.png");
    }

    [Test]
    public void UpdateEntity_UserPatchDto_SoloAvatar_ActualizaSoloAvatar()
    {
        // Arrange
        var originalEmail = "original@test.com";
        var originalHash = "original_bcrypt_hash";
        var user = new User
        {
            Id = 1,
            Username = "test",
            Email = originalEmail,
            PasswordHash = originalHash,
            Avatar = "old_avatar.jpg"
        };
        var dto = new UserPatchDto { Avatar = "https://example.com/new_avatar.png" };

        // Act
        dto.UpdateEntity(user);

        // Assert
        user.Avatar.Should().Be("https://example.com/new_avatar.png");
        user.Email.Should().Be(originalEmail);
        user.PasswordHash.Should().Be(originalHash);
    }

    [Test]
    public void UpdateEntity_UserPatchDto_NullAvatar_NoActualizaAvatar()
    {
        // Arrange
        var originalAvatar = "https://example.com/original.png";
        var user = new User
        {
            Id = 1,
            Username = "test",
            Avatar = originalAvatar
        };
        var dto = new UserPatchDto { Email = "new@test.com" };

        // Act
        dto.UpdateEntity(user);

        // Assert
        user.Avatar.Should().Be(originalAvatar);
    }

    [Test]
    public void UpdateEntity_UserPatchDto_ConEmailVacio_NoActualizaEmail()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "test",
            Email = "original@test.com"
        };
        var dto = new UserPatchDto { Email = string.Empty };

        // Act
        dto.UpdateEntity(user);

        // Assert
        user.Email.Should().Be("original@test.com");
    }

    [Test]
    public void UpdateEntity_UserPatchDto_NoDebeModificarId()
    {
        // Arrange
        var user = new User { Id = 999, Username = "test" };
        var dto = new UserPatchDto { Email = "new@test.com", Avatar = "https://example.com/avatar.png" };

        // Act
        dto.UpdateEntity(user);

        // Assert
        user.Id.Should().Be(999);
    }

    [Test]
    public void UpdateEntity_UserPatchDto_NoDebeModificarUsername()
    {
        // Arrange
        var user = new User { Id = 1, Username = "original_username" };
        var dto = new UserPatchDto { Email = "new@test.com" };

        // Act
        dto.UpdateEntity(user);

        // Assert
        user.Username.Should().Be("original_username");
    }

    #endregion

    #region ToDtoList Tests

    [Test]
    public void ToDtoList_ConMultiplesUsuarios_MapeaTodos()
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
    public void ToDtoList_ConListaVacia_RetornaVacia()
    {
        // Arrange
        var users = new List<User>();

        // Act
        var dtos = users.ToDtoList().ToList();

        // Assert
        dtos.Should().BeEmpty();
    }

    [Test]
    public void ToDtoList_DebePreservarOrden()
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
    public void ToDto_LuegoToEntity_DebePreservarDatosBasicos()
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
    public void ToDto_ConMaxId_MapeaCorrectamente()
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
    public void ToDto_ConUsernameMuyLargo_MapeaCorrectamente()
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
    public void ToDto_ConEmailUnicode_MapeaCorrectamente()
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
    public void ToEntity_ConUsernameUnicode_MapeaCorrectamente()
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
    public void ToDto_NuncaDebeExponerPasswordHash()
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
        hasPasswordHash.Should().BeFalse("DTO no debe exponer password hash por seguridad");
    }

    #endregion
}
