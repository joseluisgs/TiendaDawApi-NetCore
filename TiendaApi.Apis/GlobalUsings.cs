// Global using statements para el proyecto TiendaApi.Apis
// C# 10+ - Reduces boilerplate en cada archivo

// System namespaces más usados
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using System.Text;
global using System.Security.Claims;
global using System.IdentityModel.Tokens.Jwt;

// Microsoft namespaces más usados
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.OpenApi.Models;

// Third-party namespaces
global using CSharpFunctionalExtensions;
global using FluentValidation;
// NOTA: FluentValidation.Results se incluye individualmente
// en los archivos que necesitan ValidationException para evitar
// conflicto con TiendaApi.Apis.Exceptions.ValidationException

// Project namespaces
global using TiendaApi.Apis.Models;
global using TiendaApi.Apis.Data;
global using TiendaApi.Apis.Dtos;
global using TiendaApi.Apis.Dtos.Common;
global using TiendaApi.Apis.Errors;
global using TiendaApi.Apis.Mappers;

// NOTA: System.ComponentModel.DataAnnotations se incluye individualmente
// en los archivos que necesitan atributos de validación para evitar
// conflicto con TiendaApi.Apis.Exceptions.ValidationException
