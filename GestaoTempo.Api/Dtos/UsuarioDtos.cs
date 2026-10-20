namespace GestaoTempo.Api.Dtos;

// US01 - Cadastrar conta
public record CadastrarUsuarioRequest(string? Nome, string? Email, string? Senha);

public record UsuarioResponse(int Id, string Nome, string Email);

// US02 - Entrar
public record LoginRequest(string? Email, string? Senha);

public record LoginResponse(string Token, DateTime ExpiraEm, UsuarioResponse Usuario);
