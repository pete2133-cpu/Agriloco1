#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Agriloco.Georeferencing
{
    [Serializable] public sealed class GeoReferencePoint
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public double NormalizedMapX { get; set; }
        public double NormalizedMapY { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Source { get; set; } = "Manual";
    }
    [Serializable] public sealed class GeoTransform
    {
        public int Version { get; set; } = 1;
        public string Projection { get; set; } = "LocalEquirectangularAffine";
        public double OriginLatitude { get; set; }
        public double OriginLongitude { get; set; }
        public double EarthRadiusMeters { get; set; } = 6378137;
        public double[] X { get; set; } = new double[3];
        public double[] Y { get; set; } = new double[3];
        public double RmsErrorMeters { get; set; }
        public double[] ResidualMeters { get; set; } = new double[0];
        public void Project(double latitude, double longitude, out double x, out double y)
        {
            double east = EarthRadiusMeters * Math.Cos(OriginLatitude*Math.PI/180) * GeoCalibration.Wrap(longitude-OriginLongitude)*Math.PI/180;
            double north = EarthRadiusMeters * (latitude-OriginLatitude)*Math.PI/180;
            x=X[0]+X[1]*east+X[2]*north; y=Y[0]+Y[1]*east+Y[2]*north;
        }
    }
    public static class GeoCalibration
    {
        public static bool Finite(double d) => !double.IsNaN(d) && !double.IsInfinity(d);
        public static double Wrap(double d) => ((d+180)%360+360)%360-180;
        public static bool Coordinate(string input, bool latitude, out double result)
        {
            result=0; string s=(input??"").Trim().ToUpperInvariant();
            if(double.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out result))
                return Finite(result)&&Math.Abs(result)<=(latitude?90:180);
            var m=Regex.Match(s,@"^([+-]?\d+(?:\.\d+)?)\s*[°º]\s*(\d+(?:\.\d+)?)\s*['′]\s*(\d+(?:\.\d+)?)\s*[""″]\s*([NSEW])$");
            if(!m.Success)return false;
            double degrees=double.Parse(m.Groups[1].Value,CultureInfo.InvariantCulture), minutes=double.Parse(m.Groups[2].Value,CultureInfo.InvariantCulture), seconds=double.Parse(m.Groups[3].Value,CultureInfo.InvariantCulture);
            string direction=m.Groups[4].Value;
            if(minutes>=60||seconds>=60||degrees<0 || (latitude?!"NS".Contains(direction):!"EW".Contains(direction)))return false;
            result=(degrees+minutes/60+seconds/3600)*(direction=="S"||direction=="W"?-1:1);
            return Math.Abs(result)<=(latitude?90:180);
        }
        public static bool Valid(GeoReferencePoint p) => p!=null && !string.IsNullOrWhiteSpace(p.Id) && p.Id.Length<=64
            && !string.IsNullOrWhiteSpace(p.Name)&&p.Name.Length<=120 && (p.Source=="Manual"||p.Source=="DeviceGPS")
            && Finite(p.NormalizedMapX)&&Finite(p.NormalizedMapY)&&p.NormalizedMapX>=0&&p.NormalizedMapX<=1&&p.NormalizedMapY>=0&&p.NormalizedMapY<=1
            && Finite(p.Latitude)&&Math.Abs(p.Latitude)<=90&&Finite(p.Longitude)&&Math.Abs(p.Longitude)<=180;
        public static GeoTransform Fit(IReadOnlyList<GeoReferencePoint> points, out string status)
        {
            status="More reference points required. Recommend 4 or more distributed points.";
            if(points==null||points.Count<3)return null;
            if(points.Any(p=>!Valid(p))||points.Select(p=>p.Id).Distinct().Count()!=points.Count){status="Invalid or duplicate reference points.";return null;}
            var t=new GeoTransform {OriginLatitude=points.Average(p=>p.Latitude),OriginLongitude=Wrap(points[0].Longitude+points.Average(p=>Wrap(p.Longitude-points[0].Longitude)))};
            if(Math.Abs(t.OriginLatitude)>85){status="Calibration too close to a geographic pole.";return null;}
            int n=points.Count;double[] e=new double[n], north=new double[n];
            for(int i=0;i<n;i++){e[i]=t.EarthRadiusMeters*Math.Cos(t.OriginLatitude*Math.PI/180)*Wrap(points[i].Longitude-t.OriginLongitude)*Math.PI/180;north[i]=t.EarthRadiusMeters*(points[i].Latitude-t.OriginLatitude)*Math.PI/180;}
            if(e.Any(v=>Math.Abs(v)>50000)||north.Any(v=>Math.Abs(v)>50000)){status="Reference points are too far apart for a farm-sized local calibration.";return null;}
            double em=e.Average(),nm=north.Average(),xm=points.Average(p=>p.NormalizedMapX),ym=points.Average(p=>p.NormalizedMapY);
            double ee=0,nn=0,en=0,ex=0,nx=0,ey=0,ny=0;
            for(int i=0;i<n;i++){double a=e[i]-em,b=north[i]-nm;ee+=a*a;nn+=b*b;en+=a*b;ex+=a*(points[i].NormalizedMapX-xm);nx+=b*(points[i].NormalizedMapX-xm);ey+=a*(points[i].NormalizedMapY-ym);ny+=b*(points[i].NormalizedMapY-ym);}
            double determinant=ee*nn-en*en;
            if(ee+nn<1||determinant<=1e-8*(ee+nn)*(ee+nn)){status="Reference GPS points are coincident, too close, or nearly collinear. Spread them across the map.";return null;}
            t.X[1]=(ex*nn-nx*en)/determinant;t.X[2]=(nx*ee-ex*en)/determinant;t.X[0]=xm-t.X[1]*em-t.X[2]*nm;
            t.Y[1]=(ey*nn-ny*en)/determinant;t.Y[2]=(ny*ee-ey*en)/determinant;t.Y[0]=ym-t.Y[1]*em-t.Y[2]*nm;
            double det=t.X[1]*t.Y[2]-t.X[2]*t.Y[1];
            double norm=t.X[1]*t.X[1]+t.X[2]*t.X[2]+t.Y[1]*t.Y[1]+t.Y[2]*t.Y[2];
            if(!Finite(det)||Math.Abs(det)<=1e-8*norm){status="Map reference points are coincident or nearly collinear.";return null;}
            t.ResidualMeters=new double[n];
            for(int i=0;i<n;i++){t.Project(points[i].Latitude,points[i].Longitude,out double x,out double y);double dx=x-points[i].NormalizedMapX,dy=y-points[i].NormalizedMapY;double re=(t.Y[2]*dx-t.X[2]*dy)/det,rn=(-t.Y[1]*dx+t.X[1]*dy)/det;t.ResidualMeters[i]=Math.Sqrt(re*re+rn*rn);}
            t.RmsErrorMeters=Math.Sqrt(t.ResidualMeters.Average(r=>r*r));
            status="GPS calibration available. RMS error: "+t.RmsErrorMeters.ToString("F1",CultureInfo.InvariantCulture)+" m.";
            if(n==3)status+=" Add a fourth distributed point to independently check accuracy.";
            if(t.RmsErrorMeters>5||t.ResidualMeters.Max()>10)status+=" Check reference points: large residual errors.";
            return t;
        }
    }
}
