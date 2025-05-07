using SQLite;
using JornaPay.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JornaPay.Services
{
    public class TrabajadoresServicio
    {
        private readonly SQLiteAsyncConnection _conn;
        private static TrabajadoresServicio? _instance;
        private static readonly object _lock = new(); //Protejo el objeto ante posible concurrencia

        private TrabajadoresServicio(string dbPath)
        {
            if (_conn == null)
            {
                _conn = new SQLiteAsyncConnection(dbPath);
            }

            // Crear tablas necesarias
            _conn.CreateTableAsync<Trabajador>().Wait();
            _conn.CreateTableAsync<TrabajadorDatos>().Wait(); // Tabla para historial
            _conn.CreateTableAsync<Usuario>().Wait(); // Tabla de usuarios
        }

        public static TrabajadoresServicio GetInstance()
        {
            lock (_lock) // Evito que múltiples hilos accedan al mismo tiempo
            {
                if (_instance == null)
                {
                    string dbPath = Path.Combine(FileSystem.AppDataDirectory, "nuevostrabajadorespractica.db");

                    if (string.IsNullOrEmpty(dbPath) || !Directory.Exists(Path.GetDirectoryName(dbPath)))
                    {
                        throw new Exception("Error al acceder a la base de datos. Directorio inválido.");
                    }

                    _instance = new TrabajadoresServicio(dbPath);
                }

                return _instance;
            }
        }

        // Métodos para Trabajadores
        public async Task<List<Trabajador>> ObtenerTodosTrabajadoresAsync()
        {
            return await _conn.Table<Trabajador>().ToListAsync();
        }

        public async Task<List<Trabajador>> ObtenerTrabajadoresPorEstadoAsync(bool pagado)
        {
            return await _conn.Table<Trabajador>().Where(t => t.Pagado == pagado).ToListAsync();
        }

        public async Task<List<Trabajador>> ObtenerTrabajadoresPorUsuarioAsync(string nombreUsuario)
        {
            if (string.IsNullOrEmpty(nombreUsuario))
            {
                throw new Exception("No hay un usuario activo.");
            }
            return await _conn.Table<Trabajador>()
                              .Where(t => t.NombreUsuario == nombreUsuario)
                              .ToListAsync();
        }


        public async Task ActualizarTrabajadorAsync(Trabajador trabajador)
        {
            var trabajadorExistente = await _conn.FindAsync<Trabajador>(trabajador.Id);
            if (trabajadorExistente == null)
                throw new Exception("Trabajador no encontrado.");

            await _conn.UpdateAsync(trabajador);
        }

        public async Task EliminarTrabajadorAsync(int trabajadorId)
        {
            var trabajador = await _conn.FindAsync<Trabajador>(trabajadorId);
            if (trabajador != null)
            {
                await _conn.DeleteAsync(trabajador);
            }
        }


        public async Task CrearTrabajadorAsync(Trabajador trabajador)
        {
            var trabajadorExistente = await _conn.Table<Trabajador>()
                .FirstOrDefaultAsync(t => t.Nombre == trabajador.Nombre && t.Apellidos == trabajador.Apellidos);

            if (trabajadorExistente == null)
            {
                await _conn.InsertAsync(trabajador);
                var confirmado = await ObtenerTrabajadorAsync(trabajador.Nombre, trabajador.Apellidos);
                if (confirmado == null)
                {
                    throw new Exception("Error al guardar el trabajador en la base de datos.");
                }
            }
            else
            {
                throw new Exception($"El trabajador con nombre {trabajador.Nombre} y apellidos {trabajador.Apellidos} ya existe.");
            }
        }

        public async Task ActualizarEstadoPagoAsync(int trabajadorId, bool pagado)
        {
            var trabajador = await _conn.FindAsync<Trabajador>(trabajadorId);
            if (trabajador == null)
            {
                throw new Exception("Trabajador no encontrado.");
            }

            trabajador.Pagado = pagado;
            await _conn.UpdateAsync(trabajador);
        }

        public async Task<Trabajador?> ObtenerTrabajadorAsync(string nombre, string apellidos)
        {
            var trabajador = await _conn.Table<Trabajador>()
                .FirstOrDefaultAsync(t => t.Nombre == nombre && t.Apellidos == apellidos);

            if (trabajador == null)
            {
                throw new Exception($"No se encontró el trabajador con nombre: {nombre} y apellidos: {apellidos}");
            }

            return trabajador;
        }

        public async Task<List<Trabajador>> BuscarTrabajadoresAsync(string nombreBusqueda)
        {
            return await _conn.Table<Trabajador>()
                .Where(t => t.Nombre.Contains(nombreBusqueda) || t.Apellidos.Contains(nombreBusqueda))
                .ToListAsync();
        }

        // Métodos para TrabajadorDatos (Historial)
        public async Task<List<TrabajadorDatos>> ObtenerHistorialPorTrabajadorAsync(int trabajadorId)
        {
            var historial = await _conn.Table<TrabajadorDatos>()
                                       .Where(h => h.TrabajadorId == trabajadorId)
                                       .ToListAsync();

            return historial ?? new List<TrabajadorDatos>(); //Devuelvo la lista vacía en lugar de mostrar alerta
        }



        public async Task GuardarHistorialAsync(TrabajadorDatos registro)
        {
            await _conn.InsertAsync(registro);
        }

        public async Task ActualizarHistorialAsync(TrabajadorDatos registro)
        {
            var registroExistente = await _conn.FindAsync<TrabajadorDatos>(registro.Id);
            if (registroExistente == null)
            {
                throw new Exception("El registro no existe en la base de datos.");
            }

            await _conn.UpdateAsync(registro);
        }

        public async Task ActualizarRegistroHistorialAsync(TrabajadorDatos registro)
        {
            await _conn.UpdateAsync(registro);
        }


        public async Task EliminarHistorialAsync(int id)
        {
            var registro = await _conn.FindAsync<TrabajadorDatos>(id);
            if (registro != null)
            {
                await _conn.DeleteAsync(registro);
            }
        }


        public async Task<TrabajadorDatos?> ObtenerRegistroAsync(int id)
        {
            return await _conn.Table<TrabajadorDatos>().FirstOrDefaultAsync(t => t.Id == id);
        }


        // Métodos para Usuarios
        public async Task RegistrarUsuarioAsync(Usuario usuario)
        {
            var usuarioExistente = await _conn.Table<Usuario>()
                .FirstOrDefaultAsync(u => u.NombreUsuario == usuario.NombreUsuario);

            if (usuarioExistente != null)
            {
                throw new Exception($"El nombre de usuario {usuario.NombreUsuario} ya está en uso.");
            }

            await _conn.InsertAsync(usuario);
        }

        public async Task<Usuario?> ValidarUsuarioAsync(string nombreUsuario, string contraseña)
        {
            var usuario = await _conn.Table<Usuario>()
                .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario && u.Contraseña == contraseña);

            if (usuario != null)
            {
                // Almaceno el usuario validado en la sesión
                SesionUsuario.NombreUsuarioActual = usuario.NombreUsuario;
            }

            return usuario;
        }
    }
}