// SPDX-FileCopyrightText: 2024 DEATHB4DEFEAT <77995199+DEATHB4DEFEAT@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

import axios from 'axios';
import { titleCase } from 'title-case';

if (process.env.GITHUB_TOKEN) axios.defaults.headers.common['Authorization'] = `token ${process.env.GITHUB_TOKEN}`;
else throw new Error('BOT_TOKEN was not provided in repository secrets or GITHUB_TOKEN was not set correctly.');


// Get PR title
let prTitle = await axios.get(`https://api.github.com/repos/${process.env.GITHUB_REPOSITORY}/pulls/${process.env.PR_NUMBER}`)
    .then(res => res.data.title);

// Title case PR title
console.log(`Old PR title: ${prTitle}`);
prTitle = titleCase(prTitle);
console.log(`New PR title: ${prTitle}`);

// Update PR title
await axios.patch(`https://api.github.com/repos/${process.env.GITHUB_REPOSITORY}/pulls/${process.env.PR_NUMBER}`,
    { title: prTitle });
