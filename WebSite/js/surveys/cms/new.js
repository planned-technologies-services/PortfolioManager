({
    text: 'Content',
    text2: 'New',
    questions: [
        {
            name: 'NewContent',
            text: false,
            value: '$custom',
            required: true,
            items: {
                style: 'ListBox',
                list: [
                    { value: '$custom', text: '(custom)' },
                    { value: 'cms/oauth/oauth-wizard', text: 'Open Authentication Registration' }
                ]
            }
        }
    ],
    options: {
        materialIcon: 'note_add',
        modal: {
            fitContent: true,
            max: 'tn',
            always: true,
        },
        contentStub: false,
        discardChangesPrompt: false
    },
    submit: 'sitecontentnew.cms.app'
});