import PrimeVue from 'primevue/config';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Dialog from 'primevue/dialog';
import Toast from 'primevue/toast';
import Chip from 'primevue/chip';
import ToastService from 'primevue/toastservice';
import Tooltip from 'primevue/tooltip';
import Dropdown from 'primevue/dropdown';
import Avatar from 'primevue/avatar';
import OverlayPanel from 'primevue/overlaypanel';
import { defineNuxtPlugin } from '#app';

export default defineNuxtPlugin((nuxtApp) => {
	nuxtApp.vueApp.use(PrimeVue, { ripple: true });
	nuxtApp.vueApp.component('Button', Button);
	nuxtApp.vueApp.component('InputText', InputText);
	nuxtApp.vueApp.component('Dialog', Dialog);
	nuxtApp.vueApp.component('Toast', Toast);
	nuxtApp.vueApp.component('Chip', Chip);
	nuxtApp.vueApp.component('Dropdown', Dropdown);
	nuxtApp.vueApp.component('Avatar', Avatar);
    nuxtApp.vueApp.component('OverlayPanel', OverlayPanel);

	nuxtApp.vueApp.use(ToastService);
	nuxtApp.vueApp.directive('tooltip', Tooltip);
});
