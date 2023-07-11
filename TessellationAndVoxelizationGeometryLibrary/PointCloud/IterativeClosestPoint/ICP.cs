using System.Diagnostics;
using TVGL;

namespace TVGL.PointMatcherNet
{
    public class ICP
    {
        public ICP()
        {
            this.MatcherFactory = new KdTreeMatcherFactory();
            this.ErrorMinimizer = new PointToPlaneErrorMinimizer();
            this.Inspector = new NoOpInspector();
        }

        public IMatcherFactory MatcherFactory { get; set; }
        public IErrorMinimizer ErrorMinimizer { get; set; }

        public IInspector Inspector { get; set; }

        public EuclideanTransform Compute(
	        DataPoints readingIn,
	        DataPoints referenceIn,
	        EuclideanTransform T_refIn_dataIn)
        {
            // Create intermediate frame at the center of mass of reference pts cloud
            //  this help to solve for rotations
            var meanReference = VectorHelpers.Mean(referenceIn.points);
            EuclideanTransform T_refIn_refMean = new EuclideanTransform
            {
                translation = meanReference,
                rotation = Quaternion.Identity
            };
	
	        // Reajust reference position: 
	        // from here reference is express in frame <refMean>
	        // Shortcut to do T_refIn_refMean.inverse() * reference
            for (int i = 0; i < referenceIn.points.Length; i++)
            {
                referenceIn.points[i].point -= meanReference;
            }
	
	        // Init matcher with reference points center on its mean
            var matcher = this.MatcherFactory.ConstructMatcher(referenceIn);
	
	        return computeWithTransformedReference(readingIn, referenceIn, matcher, T_refIn_refMean, T_refIn_dataIn);
	
        }

        EuclideanTransform computeWithTransformedReference(
	        DataPoints readingIn,
	        DataPoints reference,
            IMatcher matcher,
	        EuclideanTransform T_refIn_refMean,
	        EuclideanTransform T_refIn_dataIn)
        {
	        // Reajust reading position: 
	        // from here reading is express in frame <refMean>
	        EuclideanTransform 
		        T_refMean_dataIn = T_refIn_refMean.Inverse() * T_refIn_dataIn;
	        var reading = ApplyTransformation(readingIn, T_refMean_dataIn);

            this.Inspector.Inspect(reference, "reference");

	        // Since reading and reference are express in <refMean>
	        // the frame <refMean> is equivalent to the frame <iter(0)>
	        EuclideanTransform T_iter = EuclideanTransform.Identity;
	
	        int iterationCount = 0;
            bool iterate = true;
	
            var transformationChecker = new DifferentialTransformationChecker();

	        // iterations
	        while (iterate)
	        {
		        //-----------------------------
		        // Transform Readings
		        var stepReading = ApplyTransformation(reading, T_iter);
		
                this.Inspector.Inspect(stepReading, "i" + iterationCount.ToString());

		        //-----------------------------
		        // Match to closest point in Reference
		        Matches matches = matcher.FindClosests(stepReading);
		
		        // the error minimizer's result gets tacked on to what we had before
                T_iter = ErrorMinimizerHelper.Compute(stepReading, reference, matches, this.ErrorMinimizer) * T_iter;
		
		        iterate = transformationChecker.ShouldContinue(T_iter);
	
		        ++iterationCount;
	        }
	
	        // Move transformation back to original coordinate (without center of mass)
	        // T_iter is equivalent to: T_iter(i+1)_iter(0)
	        // the frame <iter(0)> equals <refMean>
	        // so we have: 
	        //   T_iter(i+1)_dataIn = T_iter(i+1)_iter(0) * T_refMean_dataIn
	        //   T_iter(i+1)_dataIn = T_iter(i+1)_iter(0) * T_iter(0)_dataIn
	        // T_refIn_refMean remove the temperary frame added during initialization
	        return (T_refIn_refMean * T_iter * T_refMean_dataIn);
        }

        public static DataPoints ApplyTransformation(DataPoints points, EuclideanTransform transform)
        {
            var resultPoints = new DataPoint[points.points.Length];
            for (int i = 0; i < points.points.Length; i++)
            {
                var x = points.points[i];
                resultPoints[i] = new DataPoint
                    {
                        point = transform.Apply(x.point),
                        normal = x.point.Transform(transform.rotation)
                    };
            }

            return new DataPoints
            {
                points = resultPoints,
                containsNormals = points.containsNormals
            };
        }
    }
}
