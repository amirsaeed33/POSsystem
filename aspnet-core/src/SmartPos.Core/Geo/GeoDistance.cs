using System;

namespace SmartPos.Geo
{
    public static class GeoDistance
    {
        private const double EarthRadiusMeters = 6371000d;

        /// <summary>
        /// Calculates the great-circle distance between two geographic
        /// coordinates using the Haversine formula.
        /// Returns distance in meters.
        /// </summary>
        public static double MetersBetween(
            double latitude1,
            double longitude1,
            double latitude2,
            double longitude2)
        {
            ValidateCoordinates(latitude1, longitude1);
            ValidateCoordinates(latitude2, longitude2);

            var lat1 = DegreesToRadians(latitude1);
            var lat2 = DegreesToRadians(latitude2);
            var dLat = DegreesToRadians(latitude2 - latitude1);
            var dLon = DegreesToRadians(longitude2 - longitude1);

            var sinHalfLat = Math.Sin(dLat / 2d);
            var sinHalfLon = Math.Sin(dLon / 2d);

            var a = sinHalfLat * sinHalfLat
                    + Math.Cos(lat1) * Math.Cos(lat2)
                    * sinHalfLon * sinHalfLon;

            // Protect against floating-point rounding errors.
            a = Math.Max(0d, Math.Min(1d, a));

            var c = 2d * Math.Atan2(
                Math.Sqrt(a),
                Math.Sqrt(1d - a));

            return EarthRadiusMeters * c;
        }

        private static double DegreesToRadians(double degrees)
            => degrees * Math.PI / 180d;

        private static void ValidateCoordinates(
            double latitude,
            double longitude)
        {
            if (double.IsNaN(latitude) ||
                double.IsInfinity(latitude) ||
                latitude < -90d ||
                latitude > 90d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(latitude),
                    "Latitude must be between -90 and 90 degrees.");
            }

            if (double.IsNaN(longitude) ||
                double.IsInfinity(longitude) ||
                longitude < -180d ||
                longitude > 180d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(longitude),
                    "Longitude must be between -180 and 180 degrees.");
            }
        }
    }
}
