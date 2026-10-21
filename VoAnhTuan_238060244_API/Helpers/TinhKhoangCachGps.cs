namespace VoAnhTuan_238060244_API.Helpers
{
    /// <summary>
    /// Tính khoảng cách giữa 2 tọa độ GPS bằng công thức Haversine
    /// </summary>
    public static class TinhKhoangCachGps
    {
        private const double BanKinhTraiDat = 6371000; // mét

        /// <summary>
        /// Trả về khoảng cách (mét) giữa hai điểm GPS
        /// </summary>
        public static double TinhMet(double viDo1, double kinhDo1, double viDo2, double kinhDo2)
        {
            var dViDo  = DegToRad(viDo2  - viDo1);
            var dKinhDo = DegToRad(kinhDo2 - kinhDo1);

            var a = Math.Sin(dViDo / 2)  * Math.Sin(dViDo / 2) +
                    Math.Cos(DegToRad(viDo1)) * Math.Cos(DegToRad(viDo2)) *
                    Math.Sin(dKinhDo / 2) * Math.Sin(dKinhDo / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return Math.Round(BanKinhTraiDat * c, 2);
        }

        /// <summary>
        /// Kiểm tra điểm (viDo, kinhDo) có trong bán kính (mét) của trung tâm không
        /// </summary>
        public static bool TrongBanKinh(
            double viDoTrungTam, double kinhDoTrungTam,
            double viDoKiemTra,  double kinhDoKiemTra,
            double banKinhMet)
        {
            var khoangCach = TinhMet(viDoTrungTam, kinhDoTrungTam, viDoKiemTra, kinhDoKiemTra);
            return khoangCach <= banKinhMet;
        }

        private static double DegToRad(double deg) => deg * Math.PI / 180.0;
    }
}
