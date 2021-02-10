function [dataset] = SocialBoxReachStudy_InitiatorVariables_Ver8()
close all;
root = pwd;
write_location = root;

p = genpath(pwd);                                                           %Add all subfolders to this working directory
addpath(p);

%Get all Files
files = getAllFiles(root,'.csv');

OutputName = 'SocialBoxReaching_InitiatorData_16June20ExcludeSaccades.csv';

%These variables are defined in functions. Use "Control-F"
%movementPercentThreshold = 0.1;
%eyeAngleTreshold = 7.5;
%samplerate = 60; Control-F 60

%indx = Structure that holds index values.
indx.joint = 1;

JointData = struct('Pair',{},'SubNum',{},'Role',{},'TrialNum',{},'TrialID',{},'TargetName',{},'TargetLocation',{},'LocalTargetLocation',{},'TrialStartTime',{},'TrialEndTime',{},'ResponseTime',{},'InitiatorPercentGazeValid',{},...
    'InitLatency',{},'SearchStart',{},'SearchEnd',{},'LastLookTime',{},...
    'InitMoveInitTime',{},'InitMaxSpeed',{},'InitMoveEndTime',{},'InitMovementTime',{},'InitSmoothness',{},...
    'iJA',{},...
    'iGazeSeq',{},'PredSpatInfo',{},'Search_EC',{},'GazeSecStart',{},'PreJA_EC',{},'iECOp',{},'iGPL',{},'iGPL_LL',{},'iGPL_NT',{});

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
            if strcmp(data.RoleA{1},'Responder')
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
            if strcmp(data.RoleB{1},'Responder')
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
        JointData = ComputeJointData(JointData, indx.joint, cleanData);
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
    dataStruct(structIndx).InitiatorPercentGazeValid = CalculatePercentValid(dataStruct, structIndx, rawData);
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

function dataStruct = ComputeJointData(dataStruct, structIndx, rawData)
     
    dataStruct(structIndx).SearchStart = ComputeFirstLook(dataStruct,structIndx,rawData,'Cube');
    dataStruct(structIndx).InitLatency = (dataStruct(structIndx).SearchStart - dataStruct(structIndx).TrialStartTime)*1000;  
    
    [dataStruct(structIndx).SearchEnd,dataStruct(structIndx).InitMaxSpeed,dataStruct(structIndx).InitMoveEndTime] = ComputeHandMovementTime(dataStruct,structIndx,rawData); 
    dataStruct(structIndx).InitMoveInitTime = dataStruct(structIndx).SearchEnd;
    dataStruct(structIndx).InitMovementTime = dataStruct(structIndx).InitMoveEndTime-dataStruct(structIndx).InitMoveInitTime;
    dataStruct(structIndx).InitSmoothness = GetSmoothness(dataStruct,structIndx,rawData);

    if isnan(dataStruct(structIndx).SearchStart) | isnan(dataStruct(structIndx).SearchEnd)
        return;
    end
    
    dataStruct(structIndx).LastLookTime = ComputeLastLookTime(dataStruct, structIndx, rawData, dataStruct(structIndx).SearchEnd,dataStruct(structIndx).TargetName);
    
    dataStruct(structIndx).iJA = (dataStruct(structIndx).SearchEnd-dataStruct(structIndx).SearchStart)*1000;
    [dataStruct(structIndx).iGazeSeq,dataStruct(structIndx).GazeSecStart] = GetGazeSeqSaccade(dataStruct, structIndx, rawData, dataStruct(structIndx).SearchStart,dataStruct(structIndx).SearchEnd);
    
    
    try
        if contains(dataStruct(structIndx).iGazeSeq,'T')
            dataStruct(structIndx).PredSpatInfo = 1;
            dataStruct(structIndx).iGPL = (dataStruct(structIndx).SearchEnd - ComputeFirstLook(dataStruct,structIndx,rawData,dataStruct(structIndx).TargetName))*1000;
            dataStruct(structIndx).iGPL_LL = (dataStruct(structIndx).SearchEnd - dataStruct(structIndx).LastLookTime)*1000;
        else
            dataStruct(structIndx).PredSpatInfo = 0;
            dataStruct(structIndx).iGPL_NT = (dataStruct(structIndx).SearchEnd - ComputeLastLookTime(dataStruct, structIndx, rawData, dataStruct(structIndx).SearchEnd,'Cube'))*1000;
        end
    catch
        dataStruct(structIndx).PredSpatInfo = NaN;
    end

   try
        if contains(dataStruct(structIndx).iGazeSeq,'F')
            dataStruct(structIndx).iECOp = 1;
        else
            dataStruct(structIndx).iECOp = 0;
        end
    catch
       dataStruct(structIndx).iECOp = NaN;
   end
   
    dataStruct(structIndx).Search_EC = ComputePreJA_EC(dataStruct, structIndx, rawData, dataStruct(structIndx).SearchStart,dataStruct(structIndx).SearchEnd); 
    dataStruct(structIndx).PreJA_EC = ComputePreJA_EC(dataStruct, structIndx, rawData, dataStruct(structIndx).GazeSecStart,dataStruct(structIndx).SearchEnd);
    
    
end

function PreJA_EC = ComputePreJA_EC(dataStruct, structIndx, rawData, startTime, endTime)
    try

        startIndx = find(rawData.UnityTime >= startTime, 1);
        endIndx = find(rawData.UnityTime <= endTime, 1 ,'last');

        if endTime <= startTime
            percent = NaN;
        else
            hits = sum(contains(rawData.GazedObject(startIndx:endIndx),'Face') & contains(rawData.PartnerGazedObject(startIndx:endIndx),'Face'));
            total = size(rawData.JointGaze(startIndx:endIndx),1);
            percent = 100*hits/total;
        end
    catch
        percent = NaN;
    end
    
    if isempty(percent)
        percent = 0;
    end
    
    if percent > 0
        PreJA_EC = 1;
    else
        PreJA_EC = 0;
    end
end

function [string,seqStart] = GetGazeSeqSaccade(dataStruct, structIndx, rawData, startTime,endTime)
    try
        
        %Get Start and End Index
        startIndx = find(rawData.UnityTime == startTime,1);
        endIndx = find(rawData.UnityTime == endTime, 1);
        
        %Find Saccades
        saccadeState = GetSaccadeState(rawData);
        
        %If there is a saccade, get to next fixation point.
        temp = endIndx;
        endIndx = find(saccadeState(endIndx:end) ==0,1)+endIndx-1;
        
        saccadeState = saccadeState(startIndx:endIndx);
        objects = rawData.GazedObject(startIndx:endIndx);
        objects = objects(saccadeState==0);


        validIndxs = find(contains(objects,'Cube') | contains(objects,'Face'));
        validObjects = objects(validIndxs);
        
        %Equivalent of having both 'stable' and 'last' commands.
        validObjectsFlipped = flip(validObjects);
        uniqueObjs = (unique(validObjectsFlipped,'stable'));

        try
            if (contains(uniqueObjs(1),'Cube') == 0)
                if size(uniqueObjs,1) == 1
                    seqStart = ComputeLastLookTime(dataStruct, structIndx, rawData, endTime, 'Face');
                    string = 'FP'
                elseif contains(uniqueObjs(2),dataStruct(structIndx).TargetName)
                    seqStart = ComputeLastLookTime(dataStruct, structIndx, rawData, endTime, dataStruct(structIndx).TargetName);
                    string = 'TFP';
                else
                    seqStart = ComputeLastLookTime(dataStruct, structIndx, rawData, endTime, 'Face');
                    string = 'FP';
                end
            elseif contains(uniqueObjs(1),dataStruct(structIndx).TargetName)
                if size(uniqueObjs,1) == 1
                    seqStart = ComputeLastLookTime(dataStruct, structIndx, rawData, endTime, dataStruct(structIndx).TargetName);
                    string = 'OTP';
                elseif (contains(uniqueObjs(2),'Cube') == 0)
                    seqStart = ComputeLastLookTime(dataStruct, structIndx, rawData, endTime, 'Face');
                    string = 'FTP';
                else
                    seqStart = ComputeLastLookTime(dataStruct, structIndx, rawData, endTime, dataStruct(structIndx).TargetName);
                    string = 'OTP';
                end
            else
                seqStart = dataStruct(structIndx).SearchStart;
                string = 'OP';
            end
        catch
            seqStart = dataStruct(structIndx).SearchStart;
            string = 'XXX';
        end
    catch
        string = NaN;
    end
end


function [string,seqStart] = GetGazeSeq(dataStruct, structIndx, rawData, startTime,endTime)
    try
        startIndx = find(rawData.UnityTime >= startTime,1,'first');
        endIndx = find(rawData.UnityTime < endTime, 1,'last');
        objects = rawData.GazedObject(startIndx:endIndx);

        validIndxs = find(contains(objects,'Cube') | contains(objects,'Face'));
        validObjects = objects(validIndxs);
        
        %Equivalent of having both 'stable' and 'last' commands.
        validObjectsFlipped = flip(validObjects);
        uniqueObjs = (unique(validObjectsFlipped,'stable'));

        try
            if (contains(uniqueObjs(1),'Cube') == 0)
                if size(uniqueObjs,1) == 1
                    seqStart = ComputeLastLookTime(dataStruct, structIndx, rawData, endTime, 'Face');
                    string = 'FP'
                elseif contains(uniqueObjs(2),dataStruct(structIndx).TargetName)
                    seqStart = ComputeLastLookTime(dataStruct, structIndx, rawData, endTime, dataStruct(structIndx).TargetName);
                    string = 'TFP';
                else
                    seqStart = ComputeLastLookTime(dataStruct, structIndx, rawData, endTime, 'Face');
                    string = 'FP';
                end
            elseif contains(uniqueObjs(1),dataStruct(structIndx).TargetName)
                if size(uniqueObjs,1) == 1
                    seqStart = ComputeLastLookTime(dataStruct, structIndx, rawData, endTime, dataStruct(structIndx).TargetName);
                    string = 'OTP';
                elseif (contains(uniqueObjs(2),'Cube') == 0)
                    seqStart = ComputeLastLookTime(dataStruct, structIndx, rawData, endTime, 'Face');
                    string = 'FTP';
                else
                    seqStart = ComputeLastLookTime(dataStruct, structIndx, rawData, endTime, dataStruct(structIndx).TargetName);
                    string = 'OTP';
                end
            else
                seqStart = dataStruct(structIndx).SearchStart;
                string = 'OP';
            end
        catch
            seqStart = dataStruct(structIndx).SearchStart;
            string = 'OP';
        end
    catch
        string = NaN;
    end
end
%Return First Object Look Time
function time = ComputeFirstLook(dataStruct, structIndx, rawData, object,role)  

    saccadeState = GetSaccadeState(rawData);
    
    try        
        
        indx = find(contains(rawData.GazedObject,object),1);
        
        time = rawData.UnityTime(indx);
        
        if saccadeState(indx) == 1
            %Get last fixation
            finalIndx = find(saccadeState(1:indx) == 0,1,'last');
            time = rawData.UnityTime(finalIndx);
        end 
    catch
        time = NaN;
    end

    if isempty(time)
        time = NaN;
    end
end
function time = ComputeLastLookTime(dataStruct, structIndx, rawData, endTime, object)
    saccadeState = GetSaccadeState(rawData);

    try
        indxs = contains(rawData.GazedObject,object).*(rawData.UnityTime <= endTime);

        %Get Time of Last Viewed Target before Reach
        lastViewIndx = find((indxs),1,'last');

        if ~isempty(lastViewIndx)
            %Get Last Not Appearance
            lastNotIndx = find(indxs(1:lastViewIndx)==0,1,'last');
            lastViewIndx = lastNotIndx+1;
            
            if isempty(lastViewIndx)
                lastViewIndx=1;
            end
            
            if saccadeState(lastViewIndx) == 1
                    %Get last fixation
                    lastFixationIndx = find(saccadeState(1:lastViewIndx)==0,1,'last');
                    
                    time = rawData.UnityTime(lastFixationIndx);
            else
                time = rawData.UnityTime(lastViewIndx);
            end
        else
            time = rawData.UnityTime(1);
        end
        if isempty(time)
            time = NaN;
        end
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
    %speedHand = -1*speedToTarget;
    
    [maxSpeed,maxSpeedIndx] = max(speedHand(:,1)); %MAKE SURE TO REVERSE MIN TO MAX AND THE LESS/GREATER SIGNS BELOW IF SWITCHIN BACK TO SPEEDHAND
    
    criteria = movementPercentThreshold*maxSpeed;
     
    intentTimeIndx = find((speedHand(1:maxSpeedIndx,1) > criteria),1);
    endTimeIndx = find((speedHand(maxSpeedIndx:end,1) >= criteria),1,'last');
    endTimeIndx = endTimeIndx+maxSpeedIndx-1;
    
    initTime = rawData.UnityTime(intentTimeIndx+1);      %Plus 1 to take into account "diff"
    endTime = rawData.UnityTime(endTimeIndx+1);
    
    %Correct maxSpeed (remove speedToTarget component).
    %speedHand = speedHand+speedToTarget;
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





