﻿using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController : BaseApiController
{
    private readonly DataContext _context;
    private readonly ITokenService _tokenService;

    public AccountController(DataContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    [HttpPost("register")] // POST: api/account/register
    public async Task<ActionResult<UserDTO>> Register(RegisterDTO registerDTO)
    {

      if(await UserExists(registerDTO.username)) return BadRequest("Username is taken");

      using var hmac = new HMACSHA512();

      var user = new AppUser
      {
        UserName = registerDTO.username.ToLower(),
        PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDTO.password)),
        PasswordSalt = hmac.Key
      };

      _context.Users.Add(user);
      await _context.SaveChangesAsync();

      return new UserDTO
      {
        userName = user.UserName,
        token = _tokenService.CreateToken(user)
      };
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDTO>> Login(LoginDTO loginDTO)
    {
      var user = await _context.Users.SingleOrDefaultAsync(x => x.UserName == loginDTO.username);

      if(user == null) return Unauthorized("Invalid Username");

      using var hmac = new HMACSHA512(user.PasswordSalt);

      var ComputedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDTO.password));

      for (int i = 0; i < ComputedHash.Length; i++)
      {
        if (ComputedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid password");
      }

      return new UserDTO
      {
        userName = user.UserName,
        token = _tokenService.CreateToken(user)
      };
    }

    private async Task<bool> UserExists(string username)
    {
      return await _context.Users.AnyAsync(x => x.UserName == username.ToLower());
    }


}
