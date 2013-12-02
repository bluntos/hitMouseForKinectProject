using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Filter {
// Author: Ayesha Ahmad - AA
// Contains implementation of filters
// Note: Code refactoring required: Too many different variables and functions placed together
// 	- Filter should be virtual class
//	- each filter is a subclass of Filter with its own set of variables and specific function
//	- Internal variables name change: no need to name variables as 'joint---', this can be a generic class for any type of 1D time data
//	- Usage change: a separate filter must be declared for each joint/entity to be filtered
//	- Interface change: Implement information hiding - Getter Setters for all variables
	
	List<Vector3> jointsMedian;			// To keep sorted history for past inputs for MEDIAN filter
	List<Vector3> relativeJointsMedian;	// To keep sorted history for past inputs for MEDIAN filter
	Vector2 [] jointsVectorHistory;		// To keep history of vector between joint and relative joint *not currently used*
	Vector3 [] jointHistory;			// To keep history of hand positions 
	Vector3 [] relativeJointHistory;	// To keep history of relative joint, could be shoulder or elbow number of previous values of 
	
	Vector2 [] jointsVectorOutputs;		// To keep history of outputs of filter *not currently used*
	Vector3 [] jointOutputs;			// To keep history of outputs of filter
	Vector3 [] relativeJointOutputs;	// To keep history of outputs of filter
	
	// For Taylor series filter
	bool useSmoothedInput = false;		// Whether or not the filter is applied on smoothed data
	bool existsForecastedJoint = false;	// Tells whether a forecasted value exists for the joint
	Vector3 forecastedJoint;			// To store forecasted value
	bool existsForecastedRelativeJoint = false;	// Tells whether a forecasted value exists for the relative joint
	Vector3 forecastedRelativeJoint;			// To store forecasted value
	Vector3 [] TS_SmoothInputsJ;			// To keep smoothed inputs for Taylor series (joints)
	Vector3 [] TS_SmoothInputsRJ;			// To keep smoothed inputs for Taylor series

	public enum FILTER_NAME 
	{
		SIMPLE_AVG,
		MOVING_AVG,
		DOUBLE_MOVING_AVG,
		EXP_SMOOTHING,
		DOUBLE_EXP_SMOOTHING,
		ADAPTIVE_DOUBLE_EXP_SMOOTHING,
		TAYLOR_SERIES,
		MEDIAN,
		JITTER_REMOVAL,
		COMBINATION1,	// Predefined combinations of filters
		COMBINATION2,
		NONE
	};		
	public FILTER_NAME name;

	public enum JOINT_TYPE 
	{
		VECTOR,
		JOINT,
		RELATIVEJOINT
	};		
	
	public int numHistory = 10;			// Number of inputs to store in history. Higher value == more smoothing, but also more latency
	int jointIndex = 0;					// Indices into joint and relative joint histories
	int relativeJointIndex = 0;
	int jointOutputIndex = 0;			// Indices into joint and relative joint Output histories
	int relativeJointOutputIndex = 0;
	public float highestWeight = 0.7f;	// Used by Weighted Moving average filter 
	public float alpha = 0.5f;			// Used by Exponential smoothing filter, also by Jitter Removal filter for joints
	public float gamma = 0.5f;			// Used by Double Exponential smoothing filter
	public int windowSize = 2;			// Used by many filters, the amount of history to perform computations on
	float [] weights;					// Used by Moving Average filter
	float a_low = 0.2f;					// Used by Adaptive Double Exponential Smoothing Filter
	float a_high = 0.7f;
	float y_low = 0.2f;					// Used by Adaptive Double Exponential Smoothing Filter
	float y_high = 0.7f;				// "
	float v_low = 60000f;				// Used by Adaptive Double Exponential Smoothing Filter
	float v_high = 100000.0f;				// "
	
	
	// Keep history of previous ten values
	/// <summary>
	/// Initializes a new instance of the <see cref="Filter"/> class.
	/// </summary>
	public Filter(){
		jointHistory = new Vector3 [numHistory];
		relativeJointHistory = new Vector3 [numHistory];
		jointsVectorHistory = new Vector2 [numHistory];
		
		jointOutputs = new Vector3 [numHistory];
		relativeJointOutputs = new Vector3 [numHistory];
		jointsVectorOutputs= new Vector2 [numHistory];

		jointsMedian = new List<Vector3>();
		relativeJointsMedian = new List<Vector3>();
		
		forecastedJoint = new Vector3(0,0,0);
		forecastedRelativeJoint = new Vector3(0,0,0);

		weights = new float [numHistory];
		name = FILTER_NAME.MOVING_AVG;

		float tempWeight = highestWeight;
		float sumWeights = 0f;
		
		for (int i = 0; i < numHistory; i++) {
			jointHistory[i] = relativeJointHistory[i] = jointsVectorHistory[i] = Vector3.zero;
			
			if(i != numHistory-1) {
				weights[i] = tempWeight;
				sumWeights += weights[i];
				tempWeight = (1 - sumWeights)/2;
			}
			else {
				weights[i] = 1 - sumWeights;
			}
			//Debug.Log ("weight: " + i + " " + weights[i]);
		}
		
	}
	
	// AA: Interface function for filtering of individual joints
	public  Vector3 Update(Vector3 jointPos, JOINT_TYPE jointType ) {
		Vector3 newJointPos = Vector3.zero;

		if(name == FILTER_NAME.NONE) 
		{
			return jointPos;
		}
		
		switch (jointType) {
		
		case JOINT_TYPE.JOINT:
			// Loop arond the joint history buffers if necessary
			if(jointIndex > numHistory - 1) 
				jointIndex = jointIndex % numHistory;
			
			if(jointOutputIndex > numHistory - 1) 
				jointOutputIndex = jointOutputIndex % numHistory;
			
			
			// Store input in joint's history
			jointHistory[jointIndex] = jointPos;
			newJointPos = applyFilter(jointHistory, jointIndex, jointType, jointOutputs, name);

			jointIndex++;
			
			// Store the filter output value in the joint's history
			jointOutputs[jointOutputIndex] = newJointPos;
			jointOutputIndex++;
			break;

		case JOINT_TYPE.RELATIVEJOINT:
			// Loop arond the joint history buffers if necessary
			if(relativeJointIndex > numHistory - 1) 
				relativeJointIndex = relativeJointIndex % numHistory;

			if(relativeJointOutputIndex > numHistory - 1) 
				relativeJointOutputIndex = relativeJointOutputIndex % numHistory;
			
			// Store input in joint's history
			relativeJointHistory[relativeJointIndex] = jointPos;
			newJointPos = applyFilter(relativeJointHistory, relativeJointIndex, jointType, relativeJointOutputs, name);
			
			relativeJointIndex++;

			// Store the filter output value in the joint's history
			relativeJointOutputs[relativeJointOutputIndex] = newJointPos;
			relativeJointOutputIndex++;

			break;
		default:
		break;

		}	
		
		return newJointPos;
	}

	private Vector3 applyFilter(Vector3 [] array, int arrIndex,  JOINT_TYPE jointType, Vector3 [] arrayOutput = null, Filter.FILTER_NAME fname = Filter.FILTER_NAME.NONE) {
		
		Vector3 sum = Vector3.zero;

		switch (fname) {

		// Simplest joint filter, where the filter output is the average of N recent inputs
		case FILTER_NAME.SIMPLE_AVG:
			
			for (int i = 0; i < numHistory; i++) {
				sum = sum + array[i];
			}
			sum/=numHistory;
			break;

		// Moving average (MA) filters are a special case of ARMA filters where the auto regressive term is zero
		case FILTER_NAME.MOVING_AVG:
			
			for (int i = 0; i < numHistory; i++) {
				sum = sum + array[i]*weights[i];
			}
			break;
			
		// Double moving average with window size = 2 (for fast implementation)
		case FILTER_NAME.DOUBLE_MOVING_AVG:
			sum = (5.0f/9)*array[getIndex(arrIndex)] +
				  (4.0f/9)*array[getIndex(arrIndex-1)] +
					(1.0f/3)*array[getIndex(arrIndex-2)] -
					(2.0f/9)*array[getIndex(arrIndex-3)] -
					(1.0f/9)*array[getIndex(arrIndex-4)];
			break;

		// Exponential Smoothing with window size = 2 (for fast implementation)
		case FILTER_NAME.EXP_SMOOTHING:
// The dynamic code works very slowly:
//			for (int i = 0; i < windowSize; i++) {
//				sum += Mathf.Pow((1-alpha), i )*array[getIndex(arrIndex - i)];
//			}
//			sum = alpha*sum;

			// The unrolled loop for window size of two code:
			sum = alpha*array[getIndex(arrIndex)] + (1-alpha)*array[getIndex(arrIndex - 1)] + (1-alpha)*(1-alpha)*array[getIndex(arrIndex - 2)];
			break;

		// Double Exponential Smoothing with window size = 2 (for fast implementation)
		case FILTER_NAME.DOUBLE_EXP_SMOOTHING:
			if(jointType == JOINT_TYPE.JOINT) 
			{
				Vector3 trend = gamma*(arrayOutput[getIndex(jointOutputIndex-1)] -arrayOutput[getIndex(jointOutputIndex-2)]) + (1-gamma)*(gamma*(arrayOutput[getIndex(jointOutputIndex-3)] -arrayOutput[getIndex(jointOutputIndex-4)]));
				sum = alpha*array[getIndex(arrIndex)] + (1-alpha)*(arrayOutput[getIndex(arrIndex - 2)] + trend);
			}
			else 
			{
				Vector3 trend = gamma*(arrayOutput[getIndex(relativeJointOutputIndex-1)] -arrayOutput[getIndex(relativeJointOutputIndex-2)]) + (1-gamma)*(gamma*(arrayOutput[getIndex(relativeJointOutputIndex-3)] -arrayOutput[getIndex(relativeJointOutputIndex-4)]));
				sum = alpha*array[getIndex(arrIndex)] + (1-alpha)*(arrayOutput[getIndex(arrIndex - 2)] + trend);
			}
		
			break;
		
		// Median filter
		case FILTER_NAME.MEDIAN:
			if (jointType == JOINT_TYPE.JOINT) 
			{
				sum = findMedian(jointsMedian, array, arrIndex);
			}
			else if (jointType == JOINT_TYPE.RELATIVEJOINT) 
			{
				sum = findMedian(relativeJointsMedian, array, arrIndex);
			}
			
			break;
			
		// Hand joint filtered by simple avg, shoulder joint by median
		case FILTER_NAME.COMBINATION1:
			if (jointType == JOINT_TYPE.JOINT) 
			{
				sum = applyFilter(array, arrIndex, jointType, arrayOutput, Filter.FILTER_NAME.SIMPLE_AVG);
			}
			else if (jointType == JOINT_TYPE.RELATIVEJOINT) 
			{
				sum = findMedian(relativeJointsMedian, array, arrIndex);
			}
			
			break;

		// Hand joint filtered by double moving average, shoulder joint by median
		case FILTER_NAME.COMBINATION2:
			if (jointType == JOINT_TYPE.JOINT) 
			{
				sum = applyFilter(array, arrIndex, jointType, arrayOutput, Filter.FILTER_NAME.DOUBLE_MOVING_AVG);
			}
			else if (jointType == JOINT_TYPE.RELATIVEJOINT)
			{
				sum = findMedian(relativeJointsMedian, array, arrIndex);
			}
			
			break;

		// Jitter removal filter is like a simple case of moving average filter using only current and previous input
		case FILTER_NAME.JITTER_REMOVAL:
				sum = alpha*array[arrIndex] + (1- alpha)*array[getIndex(arrIndex-1)];
			break;

		// ADAPTIVE_DOUBLE_EXP_SMOOTHING filter uses a_low and a_high (alpha low and alpha high values)
		case FILTER_NAME.ADAPTIVE_DOUBLE_EXP_SMOOTHING:
			Vector3 diff = array[arrIndex] - array[getIndex(arrIndex-1)];
			Vector2 velocity = new Vector2(diff.x, diff.y);
			
			float vn = velocity.sqrMagnitude;
			if(vn < v_low) 
			{
				alpha = a_low;
				gamma = y_low;
			}
			else if(vn > v_high) 
			{
				alpha = a_high;
				gamma = y_high;
			}
			else
			{
				alpha = a_high + ((vn - v_high)/(v_low - v_high)) *(a_low - a_high);
				gamma = y_high + ((vn - v_high)/(v_low - v_high)) *(y_low - y_high);
			}
			
			if(jointType == JOINT_TYPE.JOINT) 
			{
				Vector3 myTrend = gamma*(arrayOutput[getIndex(jointOutputIndex-1)] -arrayOutput[getIndex(jointOutputIndex-2)]) + (1-gamma)*(gamma*(arrayOutput[getIndex(jointOutputIndex-3)] -arrayOutput[getIndex(jointOutputIndex-4)]));
				sum = alpha*array[getIndex(arrIndex)] + (1-alpha)*(arrayOutput[getIndex(arrIndex - 2)] + myTrend);
			}
			else 
			{
				Vector3 myTrend = gamma*(arrayOutput[getIndex(relativeJointOutputIndex-1)] -arrayOutput[getIndex(relativeJointOutputIndex-2)]) + (1-gamma)*(gamma*(arrayOutput[getIndex(relativeJointOutputIndex-3)] -arrayOutput[getIndex(relativeJointOutputIndex-4)]));
				sum = alpha*array[getIndex(arrIndex)] + (1-alpha)*(arrayOutput[getIndex(arrIndex - 2)] + myTrend);
			}
			break;
		// Taylor series can forecast 1 data point
//		case FILTER_NAME.TAYLOR_SERIES:
//			if(useSmoothedInput) 
//			{
//				// Populate the smoothed inputs array for joint/relative joint
//				Vector2 newJointPos = applyFilter(array, arrIndex, jointType, arrayOutput, FILTER_NAME.SIMPLE_AVG);
//				
//				sum = array[arrIndex] - 3*array[getIndex(arrIndex-1)] + 3*array[getIndex(arrIndex-2)] - array[getIndex(arrIndex-3)];
//
//				if(jointType == JOINT_TYPE.JOINT) 
//				{
//					TS_SmoothInputsJ[jointOutputIndex] = newJointPos;
//					sum = (8.0f/3)*TS_SmoothInputsJ[arrIndex] - (5.0f/2)* TS_SmoothInputsJ[getIndex(arrIndex-1)] + TS_SmoothInputsJ[getIndex(arrIndex-2)] - (1.0f/6)*TS_SmoothInputsJ[getIndex(arrIndex-3)];
//				}
//				else 
//				{
//					TS_SmoothInputsRJ[relativeJointOutputIndex] = newJointPos;
//					sum = (8.0f/3)*TS_SmoothInputsRJ[arrIndex] - (5.0f/2)* TS_SmoothInputsRJ[getIndex(arrIndex-1)] + TS_SmoothInputsRJ[getIndex(arrIndex-2)] - (1.0f/6)*TS_SmoothInputsRJ[getIndex(arrIndex-3)];
//
//				}
//				
//				
//			}
			
		default:
		break;
		}
	
		return sum;
	}
	
	// Finds the median from the passed list
	private Vector3 findMedian(List<Vector3> v, Vector3 [] array, int currentIndex) 
	{
		Vector3 sum = new Vector3(0,0,0);
		// First add the joint to the median list
		if(v.Count < numHistory ) 
		{
			v.Add(array[currentIndex]);
		}
		else {
			v.Remove (array[getIndex(currentIndex+1)]);// Remove the oldest input
			v.Add (array[currentIndex]);				// Add the new value
		}
		vectorComparer vc = new vectorComparer();
		v.Sort(vc);
		
		if (v.Count == 1) {
			sum = v[0];
		}
		else if(v.Count % 2 == 0) {
			sum = (v[v.Count/2 - 1] + v[v.Count/2 ])/2;
		}
		else {
			sum = v[v.Count/2 + 1];
		}
		return sum;
			
	}
	
	// AA: Interface function for filtering vector - only jitter removal filter is applied to the vector
	// (More complex filters are applied to joints)
	public Vector2 Update(Vector2 previousVector, Vector2 currentVector, float weightingFactor) {
		
		if(name == FILTER_NAME.NONE) 
		{
			return ApplyJitterRemovalFilter(previousVector, currentVector, 1.0f);
		}
		else 
		{
			return ApplyJitterRemovalFilter(previousVector, currentVector, weightingFactor);
		}
		
	}
	
	// Compute new vector as weighted sum of new and previous vector
	private Vector2 ApplyJitterRemovalFilter(Vector2 relPos, Vector2 newPos, float filterFactor)
	{
		Vector2 temp = Vector2.zero;
		temp.x = (1.0f - filterFactor) * relPos.x + filterFactor * newPos.x;
		temp.y = (1.0f - filterFactor) * relPos.y + filterFactor * newPos.y;
		return temp;
	}
	
	// Controls circular array traversal
	private int getIndex(int index) 
	{
		if(index <0) {
			return index + numHistory;
		}
		else if(index >= numHistory) {
			return index - numHistory;
		}
		else
			return index;
	}
	
}

// For performing Sort() on vectors for median filter, sorting is based on comparison of squared magnitude
public class vectorComparer: IComparer<Vector3>
{
    public int Compare(Vector3 v1, Vector3 v2)
    {
		Vector2 x = v1;
		Vector2 y = v2;
		
		if (x.sqrMagnitude > y.sqrMagnitude) 
		{
           return 1;	// x is greater
		}
		else if(x.sqrMagnitude == y.sqrMagnitude) 
		{
			return 0;	// they are equal
		}
		else {
			return -1;	// y is greater
		}
	}
}

