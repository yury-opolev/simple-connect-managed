import { CallClient } from '@azure/communication-calling';
import { AzureCommunicationTokenCredential } from '@azure/communication-common';

let callClient = null;
let callAgent = null;
let currentCall = null;
let localVideoStream = null;
let localVideoRenderer = null;
let localVideoContainerId = null;
let dotNetHelper = null;
const remoteRenderers = new Map();
const subscribedAvailability = new WeakSet();

export async function initCallAgent(acsToken, displayName) {
    if (!acsToken || typeof acsToken !== 'string') {
        throw new Error('Invalid session credentials. Please use a fresh invite link.');
    }

    callClient = new CallClient();
    const tokenCredential = new AzureCommunicationTokenCredential(acsToken);
    callAgent = await callClient.createCallAgent(tokenCredential, { displayName: displayName || 'Guest' });

    callAgent.on('callsUpdated', (e) => {
        for (const call of e.added) {
            subscribeToCall(call);
        }
    });
}

export async function joinRoom(roomId) {
    if (!callAgent) {
        throw new Error('Call agent not initialized');
    }

    const locator = { roomId: roomId };
    const callOptions = {};

    try {
        const deviceManager = await callClient.getDeviceManager();
        const cameras = await deviceManager.getCameras();

        if (cameras.length > 0) {
            const { LocalVideoStream } = await import('@azure/communication-calling');
            localVideoStream = new LocalVideoStream(cameras[0]);
            callOptions.videoOptions = { localVideoStreams: [localVideoStream] };
        }
    } catch (err) {
        console.warn('Could not access camera:', err);
    }

    currentCall = callAgent.join(locator, callOptions);
    subscribeToCall(currentCall);
}

export async function setupLocalVideo(containerId) {
    localVideoContainerId = containerId;

    if (!localVideoStream) {
        return;
    }

    const container = document.getElementById(containerId);
    if (!container) {
        return;
    }

    if (localVideoRenderer) {
        localVideoRenderer.dispose();
        localVideoRenderer = null;
    }

    container.innerHTML = '';

    const { VideoStreamRenderer } = await import('@azure/communication-calling');
    localVideoRenderer = new VideoStreamRenderer(localVideoStream);
    const view = await localVideoRenderer.createView({ scalingMode: 'Crop' });
    container.appendChild(view.target);
}

export function subscribeRemoteParticipants(dotNetRef) {
    dotNetHelper = dotNetRef;

    if (!currentCall) {
        return;
    }

    for (const participant of currentCall.remoteParticipants) {
        subscribeToParticipant(participant);
        notifyParticipantAdded(participant);
    }

    currentCall.on('remoteParticipantsUpdated', (e) => {
        for (const participant of e.added) {
            subscribeToParticipant(participant);
            notifyParticipantAdded(participant);
        }
        for (const participant of e.removed) {
            disposeRemoteRenderer(getParticipantId(participant));
            notifyParticipantRemoved(participant);
        }
    });
}

export async function renderRemoteStream(participantId, containerId) {
    if (!currentCall) {
        return;
    }

    const participant = currentCall.remoteParticipants.find(
        (p) => getParticipantId(p) === participantId
    );

    if (!participant) {
        return;
    }

    const videoStreams = participant.videoStreams.filter((s) => s.mediaStreamType === 'Video');
    if (videoStreams.length === 0) {
        return;
    }

    const stream = videoStreams[0];

    if (!stream.isAvailable) {
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('OnRemoteCameraStateChanged', false);
        }
        return;
    }

    await renderVideoStream(participantId, containerId, stream);

    if (dotNetHelper) {
        dotNetHelper.invokeMethodAsync('OnRemoteCameraStateChanged', true);
    }
}

export async function toggleMute() {
    if (!currentCall) {
        return;
    }

    if (currentCall.isMuted) {
        await currentCall.unmute();
    } else {
        await currentCall.mute();
    }
}

export async function toggleCamera() {
    if (!currentCall || !localVideoStream) {
        return;
    }

    const localStreams = currentCall.localVideoStreams;
    if (localStreams.length > 0) {
        await currentCall.stopVideo(localVideoStream);
        if (localVideoRenderer) {
            localVideoRenderer.dispose();
            localVideoRenderer = null;
        }
    } else {
        await currentCall.startVideo(localVideoStream);
        if (localVideoContainerId) {
            await setupLocalVideo(localVideoContainerId);
        }
    }
}

export async function hangUp() {
    for (const [, renderer] of remoteRenderers) {
        renderer.dispose();
    }
    remoteRenderers.clear();

    if (currentCall) {
        await currentCall.hangUp();
        currentCall = null;
    }

    if (localVideoRenderer) {
        localVideoRenderer.dispose();
        localVideoRenderer = null;
    }

    if (callAgent) {
        callAgent.dispose();
        callAgent = null;
    }

    localVideoStream = null;
    localVideoContainerId = null;
    callClient = null;
    dotNetHelper = null;
}

// --- Internal helpers ---

function subscribeToCall(call) {
    call.on('stateChanged', () => {
        if (call.state === 'Disconnected' && dotNetHelper) {
            dotNetHelper.invokeMethodAsync('OnCallDisconnected');
        }
    });
}

function subscribeToParticipant(participant) {
    const id = getParticipantId(participant);

    for (const stream of participant.videoStreams) {
        if (stream.mediaStreamType === 'Video') {
            subscribeToVideoAvailability(id, stream);
        }
    }

    participant.on('videoStreamsUpdated', (e) => {
        for (const stream of e.added) {
            if (stream.mediaStreamType === 'Video') {
                subscribeToVideoAvailability(id, stream);
            }
        }
    });
}

function subscribeToVideoAvailability(participantId, stream) {
    if (subscribedAvailability.has(stream)) {
        return;
    }
    subscribedAvailability.add(stream);

    stream.on('isAvailableChanged', async () => {
        const containerId = `remote-video-${participantId}`;

        if (stream.isAvailable) {
            await renderVideoStream(participantId, containerId, stream);
            if (dotNetHelper) {
                dotNetHelper.invokeMethodAsync('OnRemoteCameraStateChanged', true);
            }
        } else {
            disposeRemoteRenderer(participantId);
            const container = document.getElementById(containerId);
            if (container) {
                container.innerHTML = '';
            }
            if (dotNetHelper) {
                dotNetHelper.invokeMethodAsync('OnRemoteCameraStateChanged', false);
            }
        }
    });
}

async function renderVideoStream(participantId, containerId, stream) {
    const container = document.getElementById(containerId);
    if (!container) {
        return;
    }

    disposeRemoteRenderer(participantId);
    container.innerHTML = '';

    const { VideoStreamRenderer } = await import('@azure/communication-calling');
    const renderer = new VideoStreamRenderer(stream);
    const view = await renderer.createView({ scalingMode: 'Fit' });
    remoteRenderers.set(participantId, renderer);
    container.appendChild(view.target);
}

function disposeRemoteRenderer(participantId) {
    const renderer = remoteRenderers.get(participantId);
    if (renderer) {
        renderer.dispose();
        remoteRenderers.delete(participantId);
    }
}

function notifyParticipantAdded(participant) {
    if (dotNetHelper) {
        const id = getParticipantId(participant);
        dotNetHelper.invokeMethodAsync('OnParticipantAdded', id);
    }
}

function notifyParticipantRemoved(participant) {
    if (dotNetHelper) {
        const id = getParticipantId(participant);
        dotNetHelper.invokeMethodAsync('OnParticipantRemoved', id);
    }
}

function getParticipantId(participant) {
    const identifier = participant.identifier;
    return identifier.communicationUserId || identifier.rawId || 'unknown';
}
