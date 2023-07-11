using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TVGL.PointMatcherNet
{
    public class DifferentialTransformationChecker 
    {
        private List<EuclideanTransform> transforms = new List<EuclideanTransform>();
        private int smoothLength = 3;
        private float minDiffRotErr = 0.001f;
        private float minDiffTransErr = 1.0f; //0.001f;

        // TODO: make params settable by constructor?

        public bool ShouldContinue(EuclideanTransform transform)
        {
            transforms.Add(transform);
	
            double rotErr = 0, transErr = 0;

	        if(this.transforms.Count > smoothLength)
	        {
		        for(int i = transforms.Count-1; i >= transforms.Count-smoothLength; i--)
		        {
                    //Compute the mean derivative
                    rotErr += Math.Abs(VectorHelpers.AngularDistance(transforms[i].rotation, transforms[i-1].rotation));
                    transErr += (transforms[i].translation - transforms[i-1].translation).Length();
		        }

		        if(rotErr / smoothLength < this.minDiffRotErr && transErr / smoothLength < this.minDiffTransErr)
			        return false;
	        }
	
	        if (double.IsNaN(rotErr))
		        throw new ArithmeticException("abs rotation norm not a number");
	        if (double.IsNaN(transErr))
                throw new ArithmeticException("abs translation norm not a number");

            return true;
        }
    }


}
