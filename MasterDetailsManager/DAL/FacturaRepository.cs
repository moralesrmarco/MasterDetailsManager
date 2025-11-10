using DAL.Repositories;
using Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace DAL
{
    public class FacturaRepository
    {
        public readonly DBConnection dBConnection = new DBConnection();

        public DataSet ExecuteQuery(string sql)
        {
            /*
           USING:es una estructura de control que se utiliza para manejar correctamente recursos que deben
            ser liberados, como conexiones a base de datos, archivos, streams, etc.
             
             */
            using (var conn = dBConnection.GetConnection())
            {
                conn.Open();
                MySqlDataAdapter dataAdapter = new MySqlDataAdapter(sql, conn);
                DataSet dataSet = new DataSet();
                dataAdapter.Fill(dataSet, "Factura");
                return dataSet;
            }
        }

        public int InsertarFacturaConDetalles(Factura factura, List<FacturaDetalle> detalles)
        {
            if (factura == null || detalles == null || detalles.Count == 0)
                throw new ArgumentException("La factura o los detalles no son válidos.");

            using (var conn = dBConnection.GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        /*
                         Cuando colocas una @ antes de una cadena de texto, estás indicando que esa es una 
                        cadena verbatim.

                         */
                        var insertFacturaSql = @"INSERT INTO factura (Cliente, Fecha, state) 
                                         VALUES (@Cliente, @Fecha, 1);
                                         SELECT LAST_INSERT_ID();";

                        int facturaId;
                        using (var commandFactura = new MySqlCommand(insertFacturaSql, conn, transaction))
                        {
                            commandFactura.Parameters.AddWithValue("@Cliente", factura.Cliente);
                            commandFactura.Parameters.AddWithValue("@Fecha", factura.Fecha);
                            facturaId = Convert.ToInt32(commandFactura.ExecuteScalar());
                        }
                        var insertDetalleSql = @"INSERT INTO facturadetalle (FacturaId, Producto, Cantidad, Precio) 
                                         VALUES (@FacturaId, @Producto, @Cantidad, @Precio)";
                        foreach (var detalle in detalles)
                        {
                            using (var commandDetalle = new MySqlCommand(insertDetalleSql, conn, transaction))
                            {
                                commandDetalle.Parameters.AddWithValue("@FacturaId", facturaId);
                                commandDetalle.Parameters.AddWithValue("@Producto", detalle.Producto);
                                commandDetalle.Parameters.AddWithValue("@Cantidad", detalle.Cantidad);
                                commandDetalle.Parameters.AddWithValue("@Precio", detalle.Precio);
                                commandDetalle.ExecuteNonQuery();
                            }

                        }
                        transaction.Commit();
                        return facturaId;  // 🔹 Devuelve el ID de la factura insertada
                    }
                    catch (Exception ex)
                    {

                        transaction.Rollback();
                        throw new Exception("Error al insertar la factura y detalles. Transacción revertida.", ex);

                    }
                }
            }

        }

        public void ActualizarFacturaConDetalles(Factura factura, List<FacturaDetalle> nuevos, List<FacturaDetalle> actualizados, List<FacturaDetalle> eliminados)
        {
            using (var conn = dBConnection.GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Actualizar la factura principal
                        var queryFactura = @"UPDATE factura SET Cliente = @Cliente, Fecha = @Fecha WHERE Id = @Id";
                        using (var command = new MySqlCommand(queryFactura, conn, transaction))
                        {
                            command.Parameters.AddWithValue("@Cliente", factura.Cliente);
                            command.Parameters.AddWithValue("@Fecha", factura.Fecha);
                            command.Parameters.AddWithValue("@Id", factura.Id);
                            command.ExecuteNonQuery();
                        }

                        // Eliminar detalles
                        foreach (var detalle in eliminados)
                        {
                            var queryEliminar = "DELETE FROM facturadetalle WHERE Id = @Id";
                            using (var command = new MySqlCommand(queryEliminar, conn, transaction))
                            {
                                command.Parameters.AddWithValue("@Id", detalle.Id);
                                command.ExecuteNonQuery();
                            }
                        }

                        // Insertar nuevos detalles
                        foreach (var detalle in nuevos)
                        {
                            var queryInsertar = @"INSERT INTO facturadetalle (FacturaId, Producto, Cantidad, Precio)
                                          VALUES (@FacturaId, @Producto, @Cantidad, @Precio)";
                            using (var command = new MySqlCommand(queryInsertar, conn, transaction))
                            {
                                command.Parameters.AddWithValue("@FacturaId", factura.Id);
                                command.Parameters.AddWithValue("@Producto", detalle.Producto);
                                command.Parameters.AddWithValue("@Cantidad", detalle.Cantidad);
                                command.Parameters.AddWithValue("@Precio", detalle.Precio);
                                command.ExecuteNonQuery();
                            }
                        }

                        // Actualizar detalles existentes
                        foreach (var detalle in actualizados)
                        {
                            var queryActualizar = @"UPDATE facturadetalle 
                                            SET Producto = @Producto, Cantidad = @Cantidad, Precio = @Precio 
                                            WHERE Id = @Id";
                            using (var command = new MySqlCommand(queryActualizar, conn, transaction))
                            {
                                command.Parameters.AddWithValue("@Producto", detalle.Producto);
                                command.Parameters.AddWithValue("@Cantidad", detalle.Cantidad);
                                command.Parameters.AddWithValue("@Precio", detalle.Precio);
                                command.Parameters.AddWithValue("@Id", detalle.Id);
                                command.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception("Error al actualizar la factura y detalles. Transacción revertida.", ex);
                    }
                }
            }
        }


        public List<FacturaDetalle> ObtenerDetalleporFactura(int facturaId)
        {
            var detalles = new List<FacturaDetalle>();
            using (var con = dBConnection.GetConnection())
            {
                con.Open();
                var query = "select Id, FacturaId, Producto, Cantidad,Precio,Cantidad * Precio as Total from facturadetalle" +
                    "  WHERE FacturaId= @FacturaId ";
                using (var command = new MySqlCommand(query, con))
                {
                    command.Parameters.AddWithValue("@FacturaId", facturaId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            detalles.Add(new FacturaDetalle
                            {
                                Id = reader.GetInt32("Id"),
                                FacturaId = reader.GetInt32("FacturaId"),
                                Producto = reader.GetString("Producto"),
                                Cantidad = reader.GetInt32("Cantidad"),
                                Precio = reader.GetDecimal("Precio"),
                                Total = reader.GetDecimal("Total")

                            });
                        }
                    }
                }
                return detalles;
            }

        }

        public void AnularFactura(Factura factura)
        {
            using (var conn = dBConnection.GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        var queryFactura = @"UPDATE factura SET state = 0 WHERE Id = @Id";
                        using (var command = new MySqlCommand(queryFactura, conn, transaction))
                        {
                            command.Parameters.AddWithValue("@Id", factura.Id);
                            command.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception("Error al anular la factura. Transacción revertida.", ex);
                    }
                }
            }
        }
    }
}
