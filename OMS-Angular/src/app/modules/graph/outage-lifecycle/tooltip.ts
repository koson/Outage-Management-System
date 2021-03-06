import tippy from 'tippy.js';

import {
    generateCreatedOutageTemplate,
    generateRepairCrewOutageTemplate,
    generateIsolatedOutageTemplate
} from './contents';
import { ActiveOutage, OutageLifeCycleState } from '@shared/models/outage.model';

let commandedNodeIds: string[] = [];

export const addOutageTooltip = (cy, node, outage: ActiveOutage, outageElement) => {
    if (!outage) return;

    let ref = node.popperRef();

    node.tooltip = tippy(ref, {
        content: () => {
            const div = document.createElement('div');
            div.innerHTML = generateTemplate(outage);
            const button = generateButton(outage, node, outageElement);

            div.appendChild(button);
            return div;
        },
        animation: 'scale',
        trigger: 'manual',
        placement: 'right',
        arrow: true,
        interactive: true
    });

    node.on('tap', () => {
        setTimeout(() => {
            node.tooltip.show();
        }, 0);
    });

    // hide the tooltip on zoom and pan
    cy.on('zoom pan', () => {
        setTimeout(() => {
            node.tooltip.hide();
        }, 0);
    });
}

const generateTemplate = (outage: ActiveOutage) => {
    console.log(outage);
    
    if (outage.State == OutageLifeCycleState.Created)
        return generateCreatedOutageTemplate(outage);

    if (outage.State == OutageLifeCycleState.Isolated)
        return generateIsolatedOutageTemplate(outage);

    if (outage.State == OutageLifeCycleState.Repaired)
        return generateRepairCrewOutageTemplate(outage);
}

const generateButton = (outage: ActiveOutage, node, outageElement) => {
    if (outage.State == OutageLifeCycleState.Created) {
        const button = document.createElement('button');
        button.innerHTML = "Isolate";
        let deviceType = outageElement.data('deviceType');
        if(deviceType == 'remote')
        {
            button.addEventListener('click', () =>
            {
                node.sendIsolateOutageCommand(outage.Id);
                commandedNodeIds.push(node.data('id'));
            });
        }
        else if (deviceType == 'local')
        {
            button.addEventListener('click', () =>
            {
                node.sendSendLocationIsolationCrew(outage.Id);
                commandedNodeIds.push(node.data('id'));
            });    
        }
        return button;
    }

    if (outage.State == OutageLifeCycleState.Isolated) {
        const button = document.createElement('button');
        button.innerHTML = "Send Repair Crew";
        button.addEventListener('click', () => {
            node.sendRepairCrewCommand(outage.Id);
            commandedNodeIds.push(node.data('id'));
        });
        return button;
    }

    if (outage.State == OutageLifeCycleState.Repaired) {
        if (!outage.IsResolveConditionValidated) {
            const resolveButton = document.createElement('button');
            resolveButton.innerHTML = "Resolve";
            resolveButton.disabled = true;

            const validateButton = document.createElement('button');
            validateButton.innerHTML = "Validate";
            validateButton.setAttribute('mat-stroked-button','');
            validateButton.setAttribute('color', 'primary')
            validateButton.addEventListener('click', () => {
                node.sendValidateOutageCommand(outage.Id);
                commandedNodeIds.push(node.data('id'));
                validateButton.disabled = true;
            });

            const buttonDiv = document.createElement('div');
            buttonDiv.append(resolveButton);
            buttonDiv.append(validateButton);
            return buttonDiv;
        } else {
            const resolveButton = document.createElement('button');
            resolveButton.innerHTML = "Resolve";
            resolveButton.addEventListener('click', () => {
                node.sendResolveOutageCommand(outage.Id);
                commandedNodeIds.push(node.data('id'));
            });

            const validateButton = document.createElement('button');
            validateButton.innerHTML = "Validate";
            validateButton.disabled = true;

            const buttonDiv = document.createElement('div');
            buttonDiv.append(resolveButton);
            buttonDiv.append(validateButton);
            return buttonDiv;
        }

    }
}