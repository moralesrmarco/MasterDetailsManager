using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class Factura
    {
        public int Id { get; set; }
        public string Cliente { get; set; }
        public DateTime Fecha { get; set; }
        public int state { get; set; }
        public List<FacturaDetalle> Detalles { get; set; } //= new List<FacturaDetalle>();


    }
}
