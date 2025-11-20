![Header image](https://github.com/DougChisholm/App-Mod-Assist/blob/main/repo-header.png)

# App-Mod-Assist
A project to show how GitHub coding agent can turn screenshots of legacy apps into working proof-of-concepts for cloud native Azure replacements if the legacy database schema is also provided

1. Fork this repo then open the coding agent and use app-mod-assist agent telling it "modernise my app" - making sure to replace the example screenshots and sql schema first
2. When the agent has completed the Pull Reuest with the code, accept the PR and then clone the repo in VS Code locally
3. In terminal AZ LOGIN > Set a subscription context
4. Run the deploy.sh file (ensuring the settings in the bicep files are what you want - it will have RG name, SKU, UKSOUTH etc already set)

# MSFT Workshop SE Assets (MSFT ONLY)

Slides (WIP)
https://microsofteur-my.sharepoint.com/:p:/g/personal/dchisholm_microsoft_com/IQA5JRCDV3MUTZl9n2aYBwvFAQyjO7JZTyYdBB_8hDcnoYc?e=k4vDW3

Demo video (WIP)
https://microsofteur-my.sharepoint.com/:v:/g/personal/dchisholm_microsoft_com/IQAy-e4YAJwtTqzDGnh1OFX3AbleABvMKaviY7-YUINCmPk?e=eYq8FN
