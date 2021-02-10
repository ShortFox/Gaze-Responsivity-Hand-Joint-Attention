function [dataset] = SocialBoxReachStudy_ResponderVariables_Ver5()
close all;
root = pwd;
write_location = root;

p = genpath(pwd);                                                           %Add all subfolders to this working directory
addpath(p);

%Get all Files
files = getAllFiles(root,'.csv');

OutputName = 'SocialBoxReaching_ResponderData_16June20ExcludeSaccades.csv';


%Define structure array index (e.g., aka row of output table).
%indx = Structure that holds index values.
indx.joint = 1;

referenceFile = readtable('SocialBoxReaching_InitiatorData_16June20ExcludeSaccades.csv');

JointData = struct('Pair',{},'SubNum',{},'Role',{},'TrialNum',{},'TrialID',{},'TargetName',{},'TargetLocation',{},'LocalTargetLocation',{},'TrialStartTime',{},'TrialEndTime',{},'ResponseTime',{},'PercentGazeValid',{},...
    'rEyesAttBegin',{},'rFaceAttBegin',{},'rHandAttBegin',{},'rBodyAttBegin',{},'rCubeAttBegin',{},'rTargetAttBegin',{},'rOtherAttBegin',{},...
    'rEyesAttBeforeResp',{},'rFaceAttBeforeResp',{},'rHandAttBeforeResp',{},'rBodyAttBeforeResp',{},'rCubeAttBeforeResp',{},'rTargetAttBeforeResp',{},'rOtherAttBeforeResp',{},...
    'rEyesAttEnd',{},'rFaceAttEnd',{},'rHandAttEnd',{},'rBodyAttEnd',{},'rCubeAttEnd',{},'rTargetAttEnd',{},'rOtherAttEnd',{},'PredictiveGazeEyes',{},'PredictiveGazeFace',{},...
    'rRTsacc',{},'rRTpoint',{},...
    'RespInitMove',{},'RespMaxSpeed',{},'RespEndMove',{},'RespMovementTime',{},'RespSmoothness',{},...
    'CorrectIncorrect',{});

%Loop through Files
for file_ID = 1:length(files)
    
    %Paste progress into console.
    file_ID/length(files)
    
    %Obtain raw data
    data = readtable(files{file_ID});
    
    %Obtain Trial Descriptives
    [pathstr,file_name,ext] = fileparts(files{file_ID});
    name_split = strsplit(file_name,'_');
    
    %If file does not conform to trial convention, continue.
    if size(name_split) < 4
        continue;
    end
    
    %Determine if this file should be looked at. If not, go to next file.
    if strcmp(data.Task{1},'joint')
        SubNum = name_split{2};
        
        %Only open files where player file is also the "Initiator"
        if strcmp(SubNum,'Player1')
            if strcmp(data.RoleA{1},'Initiator')
                continue;
            else
                try
                    cleanData = GetUseableRawData(data);
                catch
                    continue;
                end 
                if isempty(cleanData)
                    continue;
                end
            end 
        elseif strcmp(SubNum,'Player2')
            if strcmp(data.RoleB{1},'Initiator')
                continue;
            else
                try
                    cleanData = GetUseableRawData(data);
                catch
                    continue;
                end
                if isempty(cleanData)
                    continue;
                end
            end
        end
        
        JointData = ComputeGeneralInformation(JointData, indx.joint, cleanData, name_split);
        JointData = ComputeJointData(JointData, indx.joint, cleanData, referenceFile);
        indx.joint = indx.joint+1;
    end
end
    writetable(struct2table(JointData),OutputName);
end

%Write Basic Row Information.
function dataStruct = ComputeGeneralInformation(dataStruct, structIndx, rawData, fileNameArray)

try
    %Obtain Trial Information
    dataStruct(structIndx).Pair = str2double(fileNameArray{1}(2:end));
    dataStruct(structIndx).SubNum = 100*str2double(fileNameArray{2}(7:end))+dataStruct(structIndx).Pair;
    dataStruct(structIndx).TrialNum = str2double(fileNameArray{3});
    
    if contains(fileNameArray{5}, 'Trial') == false
        trialIDArray = strsplit(fileNameArray{6},'TrialID');
        dataStruct(structIndx).TrialID = str2double(trialIDArray{2});
    %Else, if Joint Task, add TrialID and Role
    else
        trialIDArray = strsplit(fileNameArray{5},'TrialID');
        dataStruct(structIndx).TrialID = str2double(trialIDArray{2});
        if dataStruct(structIndx).SubNum < 200
            dataStruct(structIndx).Role = rawData.RoleA{1};
        else
            dataStruct(structIndx).Role = rawData.RoleB{1};
        end
    end
    
    dataStruct(structIndx).TargetName = rawData.TargetName{1};
    dataStruct(structIndx).TargetLocation = rawData.TargetLocation(1);
    
    %Write Local Cube Position
    if dataStruct(structIndx).SubNum == 2
        if dataStruct(structIndx).TargetLocation == 1
            dataStruct(structIndx).LocalTargetLocation = 3;
        elseif dataStruct(structIndx).TargetLocation == 3
            dataStruct(structIndx).LocalTargetLocation = 1;
        else
            dataStruct(structIndx).LocalTargetLocation = 2;
        end
    else
        dataStruct(structIndx).LocalTargetLocation = dataStruct(structIndx).TargetLocation;
    end  
    
    dataStruct(structIndx).TrialStartTime = rawData.UnityTime(1);
    dataStruct(structIndx).TrialEndTime = rawData.UnityTime(end);
    dataStruct(structIndx).ResponseTime = (dataStruct(structIndx).TrialEndTime-dataStruct(structIndx).TrialStartTime)*1000;
    dataStruct(structIndx).PercentGazeValid = CalculatePercentValid(dataStruct, structIndx, rawData);
catch
end
end
function percent = CalculatePercentValid(dataStruct, structIndx, rawData)   
    gazes = rawData.GazeValid(1:end);
    percent = 100*sum(gazes,1)/size(gazes,1);
end
function cleanData = GetUseableRawData(rawData)
    
    cleanData = rawData;

%     %"Trial Start" is when participant looks away from Face.
%     cleanDataStartIndx = find(contains(rawData.GazedObject,'Face')==0,1);
%     
%     %If startIndx is still empty, skip file
%     if isempty(cleanDataStartIndx)
%         return;
%     end
%     
%     cleanData = rawData(cleanDataStartIndx:end,:);
                
end
%Write Information Relevant to Joint Task

function dataStruct = ComputeJointData(dataStruct, structIndx, rawData, refFile)
      
    try
    startTime = refFile.SearchStart(find((refFile.TrialNum == dataStruct(end).TrialNum) & (refFile.Pair==dataStruct(end).Pair)));
    gazeSecStart = refFile.GazeSecStart(find((refFile.TrialNum == dataStruct(end).TrialNum) & (refFile.Pair==dataStruct(end).Pair)));
    endTime = refFile.SearchEnd(find((refFile.TrialNum == dataStruct(end).TrialNum) & (refFile.Pair==dataStruct(end).Pair)));
    
    startIndx = find(rawData.PartnerLocalTime >= startTime, 1);
    gazeSecStartIndx = find(rawData.PartnerLocalTime >= gazeSecStart, 1);
    endIndx = find(rawData.PartnerLocalTime >= endTime, 1);
    
    startTime = rawData.UnityTime(startIndx);
    gazeSecStart = rawData.UnityTime(gazeSecStartIndx);
    endTime = rawData.UnityTime(endIndx);
    catch
        return;
    end
    
    if isempty(startTime) | isempty(endTime)
        return;
    end
    
    if isnan(startTime) | isnan(endTime)
        hello = 1;
    end
    
    dataStruct(structIndx).rEyesAttBegin = ComputePercentLookAtBodyType(dataStruct, structIndx, rawData, 'Face', 'Responder', startTime,endTime);
    dataStruct(structIndx).rFaceAttBegin = ComputePercentLookAtBodyType(dataStruct, structIndx, rawData, 'Head', 'Responder', startTime,endTime);
    
    %Predictive/NonPredictive Gaze
    PredictiveGazeEyes = ComputePercentLookAtBodyType(dataStruct, structIndx, rawData, 'Face', 'Responder', gazeSecStart,endTime);
    PredictiveGazeFace = ComputePercentLookAtBodyType(dataStruct, structIndx, rawData, 'Head', 'Responder', gazeSecStart,endTime);
    
    if (PredictiveGazeEyes > 0)
        dataStruct(structIndx).PredictiveGazeEyes = 1;
    else
        dataStruct(structIndx).PredictiveGazeEyes = 0;
    end
    
    if (PredictiveGazeFace > 0)
        dataStruct(structIndx).PredictiveGazeFace = 1;
    else
        dataStruct(structIndx).PredictiveGazeFace = 0;
    end
    
    dataStruct(structIndx).rHandAttBegin = ComputePercentLookAtBodyType(dataStruct, structIndx, rawData, 'Hand', 'Responder',  startTime,endTime);
    dataStruct(structIndx).rBodyAttBegin = ComputePercentLookAtBodyType(dataStruct, structIndx, rawData, 'Body', 'Responder',  startTime,endTime);
    dataStruct(structIndx).rCubeAttBegin = ComputePercentLookAtBodyType(dataStruct, structIndx, rawData, 'Cube', 'Responder',  startTime,endTime);
    dataStruct(structIndx).rTargetAttBegin = ComputePercentLookAtBodyType(dataStruct, structIndx, rawData, dataStruct(structIndx).TargetName, 'Responder',  startTime,endTime);
    dataStruct(structIndx).rOtherAttBegin = 100-(dataStruct(structIndx).rEyesAttBegin+dataStruct(structIndx).rHandAttBegin+dataStruct(structIndx).rBodyAttBegin+dataStruct(structIndx).rCubeAttBegin);
    
    
    dataStruct(structIndx).rRTsacc = ComputeTargetLookAfterTime(dataStruct, structIndx, rawData, startTime, endTime, 'Responder')*1000;
    %dataStruct(structIndx).rRTpoint = ComputeTargetPointAfterTime(dataStruct, structIndx, rawData, startTime, endTime, 'Responder')*1000;

    [dataStruct(structIndx).RespInitMove,dataStruct(structIndx).RespMaxSpeed,dataStruct(structIndx).RespEndMove] = ComputeHandMovementTime(dataStruct,structIndx,rawData); 
    dataStruct(structIndx).RespMovementTime = dataStruct(structIndx).RespEndMove-dataStruct(structIndx).RespInitMove;
    dataStruct(structIndx).RespSmoothness = GetSmoothness(dataStruct,structIndx,rawData);
    dataStruct(structIndx).rRTpoint = (dataStruct(structIndx).RespInitMove-endTime)*1000;
    
    
    dataStruct(structIndx).rEyesAttBeforeResp = ComputePercentLookAtBodyType(dataStruct, structIndx, rawData, 'Face', 'Responder', endTime,dataStruct(structIndx).RespInitMove);
    dataStruct(structIndx).rFaceAttBeforeResp = ComputePercentLookAtBodyType(dataStruct, structIndx, rawData, 'Head', 'Responder', endTime,dataStruct(structIndx).RespInitMove);
    dataStruct(structIndx).rHandAttBeforeResp = ComputePercentLookAtBodyType(dataStruct, structIndx, rawData, 'Hand', 'Responder',  endTime,dataStruct(structIndx).RespInitMove);
    dataStruct(structIndx).rBodyAttBeforeResp = ComputePercentLookAtBodyType(dataStruct, structIndx, rawData, 'Body', 'Responder',  endTime,dataStruct(structIndx).RespInitMove);
    dataStruct(structIndx).rCubeAttBeforeResp = ComputePercentLookAtBodyType(dataStruct, structIndx, rawData, 'Cube', 'Responder',  endTime,dataStruct(structIndx).RespInitMove);
    dataStruct(structIndx).rTargetAttBeforeResp = ComputePercentLookAtBodyType(dataStruct, structIndx, rawData, dataStruct(structIndx).TargetName, 'Responder',  endTime,dataStruct(structIndx).RespInitMove);
    dataStruct(structIndx).rOtherAttBeforeResp = 100-(dataStruct(structIndx).rEyesAttBeforeResp+dataStruct(structIndx).rHandAttBeforeResp+dataStruct(structIndx).rBodyAttBeforeResp+dataStruct(structIndx).rCubeAttBeforeResp);
    
    dataStruct(structIndx).rEyesAttEnd = ComputePercentLookAtBodyType(dataStruct, structIndx, rawData, 'Face', 'Responder',  dataStruct(structIndx).RespInitMove);
    dataStruct(structIndx).rFaceAttEnd = ComputePercentLookAtBodyType(dataStruct, structIndx, rawData, 'Head', 'Responder',  dataStruct(structIndx).RespInitMove);
    dataStruct(structIndx).rHandAttEnd = ComputePercentLookAtBodyType(dataStruct, structIndx, rawData, 'Hand', 'Responder',  dataStruct(structIndx).RespInitMove);
    dataStruct(structIndx).rBodyAttEnd = ComputePercentLookAtBodyType(dataStruct, structIndx, rawData, 'Body', 'Responder',  dataStruct(structIndx).RespInitMove);
    dataStruct(structIndx).rCubeAttEnd = ComputePercentLookAtBodyType(dataStruct, structIndx, rawData, 'Cube', 'Responder',  dataStruct(structIndx).RespInitMove);
    dataStruct(structIndx).rTargetAttEnd = ComputePercentLookAtBodyType(dataStruct, structIndx, rawData, dataStruct(structIndx).TargetName, 'Responder',  dataStruct(structIndx).RespInitMove);
    dataStruct(structIndx).rOtherAttEnd = 100-(dataStruct(structIndx).rEyesAttEnd+dataStruct(structIndx).rHandAttEnd+dataStruct(structIndx).rBodyAttEnd+dataStruct(structIndx).rCubeAttEnd);
    
    try
    %Determine if Trial was Correct.
    if (strcmp(rawData.HandContactObj{end}, dataStruct(structIndx).TargetName))
        dataStruct(structIndx).CorrectIncorrect = 1;
    else
        dataStruct(structIndx).CorrectIncorrect = 0;
    end  
    catch
        dataStruct(structIndx).CorrectIncorrect = 0;
    end
end

function percent = ComputePercentLookAtBodyType(dataStruct, structIndx, rawData, bodyType, role, startTime, endTime)

%For Head, Head is looked at if GazeAngle to eyes is within 7.5 degrees
headGazeCriteria = 7.5;


try
    %if endTime is not included included.
    if nargin == 6
        startIndx = find(rawData.UnityTime >= startTime, 1);
        endIndx = find(rawData.UnityTime == dataStruct(structIndx).TrialEndTime, 1);
    else
        startIndx = find(rawData.UnityTime >= startTime, 1);
        endIndx = find(rawData.UnityTime <= endTime, 1 ,'last');
    end
    
    if strcmp(bodyType,'Head')
        hits = size(find(rawData.GazeAngleToPartnerHead(startIndx:endIndx)<=headGazeCriteria),1);
    else
        hits = contains(rawData.GazedObject(startIndx:endIndx),bodyType);
    end
        total = size(rawData.GazedObject(startIndx:endIndx),1);
        percent = 100*sum(hits,1)/total;
catch
    percent = NaN;
end
    if isempty(percent)
        percent = 0;
    end
end

function time = ComputeTargetLookAfterTime(dataStruct, structIndx, rawData, startTime, endTime, role)
        saccadeState = GetSaccadeState(rawData);

    try
        if nargin == 6

            indx = find((contains(rawData.GazedObject,dataStruct(structIndx).TargetName)).*(rawData.UnityTime >= endTime) == 1,1);
            time = rawData.UnityTime(indx);

        if saccadeState(indx) == 1
            %Get last fixation
            finalIndx = find(saccadeState(1:indx) == 0,1,'last');
            time = rawData.UnityTime(finalIndx);
        end 
            
        else
        end
        
        if isempty(time)
            time = NaN;
        else
            time = time-endTime;
            
            %Detect when the first gaze occured.
%             if time == 0
%                 %Get Last Not Appearance
%                 time = rawData.UnityTime((find(contains(rawData.GazedObject,dataStruct(structIndx).TargetName)== 0& (rawData.UnityTime < (time+endTime)),1,'last')));
% 
%                 if ~isempty(time)    
%                     %Get Appearance from last Not Appearance
%                     time = rawData.UnityTime((find(contains(rawData.GazedObject,dataStruct(structIndx).TargetName)& (rawData.UnityTime > time),1)));
%                 else
%                     time = rawData.UnityTime(1);
%                 end
%                 
%                 time=time-endTime;
%             end
        end
    catch
        time = NaN;
    end
end

function output = GetSaccadeState(rawData)
    saccadeAngleThreshold = 60;
        
    gazeDirection = [rawData.CombinedGazeDirectionX,rawData.CombinedGazeDirectionY,rawData.CombinedGazeDirectionZ];

    P1 = gazeDirection(1:end-1,:);
    P2 = gazeDirection(2:end,:);
    
    gazeAngle = zeros(length(P1)+1,1);
    
    for gazeIndx = 2:length(P1)
        gazeAngle(gazeIndx) = atan2d(norm(cross(P1(gazeIndx-1,:),P2(gazeIndx-1,:))),dot(P1(gazeIndx-1,:),P2(gazeIndx-1,:)));
    end
    
    timediff = [0; diff(rawData.UnityTime)];
    
    gazeAngleVelocity = gazeAngle./timediff;
    gazeAngleVelocity(isnan(gazeAngleVelocity))=0;
    
    windowSize = 3; 
    b = (1/windowSize)*ones(1,windowSize);
    a = 1;
    y = filter(b,a,gazeAngleVelocity);
    
    y(y<=saccadeAngleThreshold) = 0;
    y(y>0) = 1;
    
    output = y;    
end

function time = ComputeTargetPointAfterTime(dataStruct, structIndx, rawData,startTime, endTime, role)
    try
    movementPercentThreshold = 0.05;
    posHand = [rawData.HandPosX(1:end),rawData.HandPosY(1:end),rawData.HandPosZ(1:end)];

    startIndx = find(rawData.UnityTime >= startTime, 1);
    endIndx = find(rawData.UnityTime >= endTime, 1);
   
    %% Filter Data using 4th Order Band-Pass Butterworth Filter
    [weight_b,weight_a] = butter(4,10/(60/2));
    try
        posHand = filtfilt(weight_b,weight_a,posHand);
    catch
        return;
    end
    
    trialTime = rawData.UnityTime(1:end);
    trialTimeDiff = diff(trialTime);
    trialTimeDiff = mean(trialTimeDiff);

    posHandDiff = diff(posHand,1);
    speedHand = sqrt(sum(posHandDiff.*posHandDiff,2))./trialTimeDiff;
    
    [maxSpeed,maxSpeedIndx] = max(speedHand(:,1));
    
    criteria = movementPercentThreshold*maxSpeed;
    
    intentTimeIndx = find((speedHand(1:maxSpeedIndx,1) > criteria),1);
    endTimeIndx = find((speedHand(maxSpeedIndx:end,1) < criteria),1);
    endTimeIndx = endTimeIndx+maxSpeedIndx-1;
    
    initTime = rawData.UnityTime(intentTimeIndx+1);      %Plus 1 to take into account "diff"
    endTime = rawData.UnityTime(endTimeIndx+1);
    
    time = initTime-rawData.UnityTime(endIndx);      %Plus 1 to take into account "diff"
    catch
        time = NaN;
    end

end
function smoothness = GetSmoothness(dataStruct,structIndx,rawData)

    posHand = [rawData.HandPosX(1:end),rawData.HandPosY(1:end),rawData.HandPosZ(1:end)];
    %% Filter Data using 4th Order Band-Pass Butterworth Filter
    [weight_b,weight_a] = butter(4,10/(60/2));
    try
        posHand = filtfilt(weight_b,weight_a,posHand);
    catch
        return;
    end
    
    handDiff = diff(posHand,1);
    
    %trialTime = rawData.UnityTime(1:end);
    %trialTimeDiff = diff(trialTime);
    %trialTimeDiff = mean(trialTimeDiff);
    
    speedHand = sqrt(sum(handDiff.*handDiff,2))./(1/60);
    
    %Find Movement Init Time
    movementPercentThreshold = 0.05;
    [maxSpeed,maxSpeedIndx] = max(speedHand(:,1)); %MAKE SURE TO REVERSE MIN TO MAX AND THE LESS/GREATER SIGNS BELOW IF SWITCHIN BACK TO SPEEDHAND  
    criteria = movementPercentThreshold*maxSpeed;
     
    intentTimeIndx = find((speedHand(1:maxSpeedIndx,1) > criteria),1);
    %endTimeIndx = find((speedHand(maxSpeedIndx:end,1) >= criteria),1,'last');
    %endTimeIndx = endTimeIndx+maxSpeedIndx-1;

    hello = speedHand(intentTimeIndx:end);
    smoothness = SpectralArcLength(speedHand(intentTimeIndx:end),1/60,[0.05,10,4]);
    
end
%Gets Movement Initiation Time, Max Speed, and Movement End Time.
function [initTime, maxSpeed, endTime] = ComputeHandMovementTime(dataStruct, structIndx, rawData)

    movementPercentThreshold = 0.05; 
    posHand = [rawData.HandPosX(1:end),rawData.HandPosY(1:end),rawData.HandPosZ(1:end)];
    distTarget = rawData.HandToTargetDist(1:end);
    
    %% Filter Data using 4th Order Band-Pass Butterworth Filter
    [weight_b,weight_a] = butter(4,10/(60/2));
    try
        posHand = filtfilt(weight_b,weight_a,posHand);
        distTarget = filtfilt(weight_b,weight_a,distTarget);
    catch
        return;
    end
    
    trialTime = rawData.UnityTime(1:end);
    trialTimeDiff = diff(trialTime);
    trialTimeDiff = mean(trialTimeDiff);

    posHandDiff = diff(posHand,1);
    distTargetDiff = diff(distTarget,1);
    speedHand = sqrt(sum(posHandDiff.*posHandDiff,2))./trialTimeDiff;
    speedToTarget = distTargetDiff./trialTimeDiff;
    
    %Take into account hand speed + movement towards target to consider it
    %movement initiation to target.
    %speedHand = speedHand+-1*speedToTarget;

    [maxSpeed,maxSpeedIndx] = max(speedHand(:,1)); %MAKE SURE TO REVERSE MIN TO MAX AND THE LESS/GREATER SIGNS BELOW IF SWITCHIN BACK TO SPEEDHAND
    
    criteria = movementPercentThreshold*maxSpeed;
     
    intentTimeIndx = find((speedHand(1:maxSpeedIndx,1) <= criteria),1, 'last');
    endTimeIndx = find((speedHand(maxSpeedIndx:end,1) >= criteria),1,'last');
    endTimeIndx = endTimeIndx+maxSpeedIndx-1;
    
    initTime = rawData.UnityTime(intentTimeIndx+1);      %Plus 1 to take into account "diff"
    endTime = rawData.UnityTime(endTimeIndx+1);
    
    %Correct maxSpeed (remove speedToTarget component).
    speedHand = speedHand+speedToTarget;
    maxSpeed = speedHand(maxSpeedIndx);
    
%     plot(speedHand);
%     hold on
%     plot(speedToTarget);
%     hold off
    
    if isempty(initTime)
        initTime = NaN;
    end
    
    if isempty(endTime)
        endTime = NaN;
    end     
end








