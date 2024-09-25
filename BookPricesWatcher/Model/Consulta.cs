using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookPricesWatcher.Model;

class Consulta
{
    public Book Book { get; set; }
    public DateTime DataConsulta { get; set; }
    public string Site { get; set; }
    public int Duration { get; set; }
}