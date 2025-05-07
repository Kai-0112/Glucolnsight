using System;
using System.Collections.Generic;

namespace Infrastructure.Entities;

public partial class CGMReading
{
    public long reading_id { get; set; }

    public int user_id { get; set; }

    public DateTime reading_time { get; set; }

    public int glucose_mgdl { get; set; }
}
